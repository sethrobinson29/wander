using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Auth;
using Wander.Api.Models.Users;
using Wander.Api.Services;

namespace Wander.Api.Controllers;

[ApiController]
[Route("users")]
public class UserController(
    UserManager<ApplicationUser> userManager,
    WanderDbContext db,
    TokenService tokenService,
    ActivityService activity,
    NotificationService notifications,
    AuditLogService auditLog) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Queries ──────────────────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MyProfileResponse>> GetMyProfile()
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        return Ok(new MyProfileResponse(
            user.Id,
            user.UserName!,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Pronouns,
            user.Bio,
            user.AvatarId,
            user.EmailPrivacy,
            user.FirstNamePrivacy,
            user.LastNamePrivacy,
            user.PronounsPrivacy,
            user.BioPrivacy,
            user.FollowingCountPrivacy,
            user.FollowerCountPrivacy,
            user.ActivityPrivacy,
            user.CreatedAt));
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<PublicProfileResponse>> GetProfile(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = requesterId == user.Id;
        var isFollowing = requesterId != null && !isSelf &&
                          await db.Follows.AnyAsync(f => f.FollowerId == requesterId && f.FolloweeId == user.Id);

        var followerCount = await db.Follows.CountAsync(f => f.FolloweeId == user.Id);
        var followingCount = await db.Follows.CountAsync(f => f.FollowerId == user.Id);

        var publicDecks = await db.Decks
            .Where(d => d.OwnerId == user.Id && d.Visibility == Visibility.Public)
            .Include(d => d.Cards).ThenInclude(c => c.Card)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        return Ok(new PublicProfileResponse(
            user.UserName!,
            PrivacyService.IsVisible(user.FirstNamePrivacy, isFollowing) ? user.FirstName : null,
            PrivacyService.IsVisible(user.LastNamePrivacy, isFollowing) ? user.LastName : null,
            PrivacyService.IsVisible(user.PronounsPrivacy, isFollowing) ? user.Pronouns : null,
            PrivacyService.IsVisible(user.BioPrivacy, isFollowing) ? user.Bio : null,
            user.AvatarId,
            PrivacyService.IsVisible(user.EmailPrivacy, isFollowing) ? user.Email : null,
            PrivacyService.IsVisible(user.FollowingCountPrivacy, isFollowing) ? followingCount : null,
            PrivacyService.IsVisible(user.FollowerCountPrivacy, isFollowing) ? followerCount : null,
            isFollowing,
            user.ActivityPrivacy,
            publicDecks.Select(d => new PublicDeckSummary(
                d.Id,
                d.Name,
                d.Description,
                d.Format.ToString(),
                d.Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity),
                (d.Cards.Any(c => c.IsCommander)
                    ? d.Cards.Where(c => c.IsCommander).SelectMany(c => c.Card?.ColorIdentity ?? [])
                    : d.Cards.SelectMany(c => c.Card?.ColorIdentity ?? []))
                    .Distinct()
                    .OrderBy(c => "WUBRG".IndexOf(c, StringComparison.Ordinal))
                    .ToList(),
                d.UpdatedAt)).ToList(),
            user.CreatedAt));
    }

    public record UserSearchResult(string Username, string? AvatarId, int DeckCount);

    [HttpGet("search")]
    public async Task<ActionResult<List<UserSearchResult>>> Search([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<UserSearchResult>());

        var lowerQ = q.ToLower();
        var users = await db.Users
            .Where(u => u.UserName!.ToLower().Contains(lowerQ))
            .Select(u => new UserSearchResult(
                u.UserName!,
                u.AvatarId,
                u.Decks.Count(d => d.Visibility == Visibility.Public)))
            .Take(20)
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpPost("{username}/follow")]
    [Authorize]
    public async Task<IActionResult> Follow(string username)
    {
        var target = await userManager.FindByNameAsync(username);
        if (target is null) return NotFound();
        if (target.Id == UserId) return BadRequest(new { error = "You cannot follow yourself." });

        var alreadyFollowing = await db.Follows.AnyAsync(f => f.FollowerId == UserId && f.FolloweeId == target.Id);
        if (alreadyFollowing) return NoContent(); // idempotent — double-follow is not an error

        db.Follows.Add(new UserFollow { FollowerId = UserId, FolloweeId = target.Id, CreatedAt = DateTimeOffset.UtcNow });
        activity.Record(UserId, ActivityType.FollowedUser, targetId: target.Id, targetName: target.UserName);
        await db.SaveChangesAsync();
        await notifications.NotifyAsync(
            recipientId: target.Id,
            actorId: UserId,
            type: NotificationType.Followed,
            actorUsername: User.Identity!.Name);

        return NoContent();
    }

    [HttpDelete("{username}/follow")]
    [Authorize]
    public async Task<IActionResult> Unfollow(string username)
    {
        var target = await userManager.FindByNameAsync(username);
        if (target is null) return NotFound();

        // ExecuteDeleteAsync is safe even if the row doesn't exist
        await db.Follows
            .Where(f => f.FollowerId == UserId && f.FolloweeId == target.Id)
            .ExecuteDeleteAsync();
        return NoContent();
    }

    [HttpGet("{username}/activity")]
    public async Task<ActionResult<ActivityPageResponse>> GetActivity(
    string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null) return NotFound();

        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isSelf = requesterId == user.Id;
        var isFollowing = requesterId != null && !isSelf &&
                           await db.Follows.AnyAsync(f => f.FollowerId == requesterId && f.FolloweeId == user.Id);

        if (!PrivacyService.IsVisible(user.ActivityPrivacy, isFollowing))
            return Ok(new ActivityPageResponse([], 0));   // hidden — return empty, not 403

        pageSize = Math.Clamp(pageSize, 1, 50);
        var skip = (page - 1) * pageSize;

        var total = await db.UserActivities.CountAsync(a => a.UserId == user.Id);
        var items = await db.UserActivities
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip).Take(pageSize)
            .Select(a => new ActivityItem(a.Type.ToString(), a.TargetId, a.TargetName, a.CreatedAt))
            .ToListAsync();

        return Ok(new ActivityPageResponse(items, total));
    }

    // ── Mutations ────────────────────────────────────────────────────────────

    [HttpPut("me/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        if (!AvatarService.IsValidAvatarId(request.AvatarId))
            return BadRequest(new { error = "Invalid avatar selection." });

        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        user.Pronouns = request.Pronouns?.Trim();
        user.Bio = request.Bio?.Trim();
        user.AvatarId = request.AvatarId;

        await db.SaveChangesAsync();
        await auditLog.LogAsync("user.updated.name",
            actorId: UserId, actorUsername: user.UserName);
        return NoContent();
    }

    [HttpPut("me/security")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> UpdateSecurity(UpdateSecurityRequest request)
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            return BadRequest(new { errors = new[] { "Current password is incorrect." } });

        var passwordChanged = false;
        var emailChanged = false;

        if (request.NewPassword is not null)
        {
            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            passwordChanged = true;
        }

        if (!string.Equals(user.Email, request.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (await userManager.FindByEmailAsync(request.NewEmail) is not null)
                return BadRequest(new { errors = new[] { "Email is already in use." } });

            var result = await userManager.SetEmailAsync(user, request.NewEmail);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            emailChanged = true;
        }

        if (!string.Equals(user.UserName, request.NewUsername, StringComparison.OrdinalIgnoreCase))
        {
            if (await userManager.FindByNameAsync(request.NewUsername) is not null)
                return BadRequest(new { errors = new[] { "Username is already in use." } });

            var result = await userManager.SetUserNameAsync(user, request.NewUsername);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Invalidate all existing sessions before issuing new tokens
        await db.RefreshTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync();

        if (passwordChanged)
            await auditLog.LogAsync("user.updated.password",
                actorId: UserId, actorUsername: user.UserName);

        if (emailChanged)
            await auditLog.LogAsync("user.updated.email",
                actorId: UserId, actorUsername: user.UserName);

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPut("me/privacy")]
    [Authorize]
    public async Task<IActionResult> UpdatePrivacy(UpdatePrivacyRequest request)
    {
        var user = await userManager.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        user.EmailPrivacy = request.EmailPrivacy;
        user.FirstNamePrivacy = request.FirstNamePrivacy;
        user.LastNamePrivacy = request.LastNamePrivacy;
        user.PronounsPrivacy = request.PronounsPrivacy;
        user.BioPrivacy = request.BioPrivacy;
        user.FollowingCountPrivacy = request.FollowingCountPrivacy;
        user.FollowerCountPrivacy = request.FollowerCountPrivacy;
        user.ActivityPrivacy = request.ActivityPrivacy;

        await db.SaveChangesAsync();
        await auditLog.LogAsync("user.updated.privacy",
            actorId: UserId, actorUsername: user.UserName);
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Verbatim copy of AuthController.IssueTokensAsync — needed here to re-issue tokens
    // after a security change without creating a shared dependency on AuthController
    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAt) = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken(user.Id);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken.Token, expiresAt);
    }
}