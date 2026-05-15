using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Infrastructure.Scryfall;
using Wander.Api.Models.Admin;
using Wander.Api.Services;

namespace Wander.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    ScryfallBulkDataService syncService,
    UserManager<ApplicationUser> userManager,
    WanderDbContext db,
    IAuditLogService auditLog) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private string ActorUsername => User.Identity?.Name ?? "";

    // ── Jobs ─────────────────────────────────────────────────────────────────

    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        await syncService.SyncAsync(cancellationToken);
        return Ok(new { message = "Sync complete." });
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<ActionResult<AdminUserListResponse>> GetUsers(
        [FromQuery] string? q = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string sort = "createdAt:desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var adminRoleId = await db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var query = db.Users.AsQueryable();

        if (role == "admin" && adminRoleId != null)
            query = query.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));
        else if (role == "user" && adminRoleId != null)
            query = query.Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId));

        if (status == "active")
            query = query.Where(u => !u.IsDeactivated);
        else if (status == "suspended")
            query = query.Where(u => u.IsDeactivated);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(lower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(lower)) ||
                u.Email!.ToLower().Contains(lower) ||
                u.UserName!.ToLower().Contains(lower));
        }

        query = sort switch
        {
            "name:asc"       => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            "name:desc"      => query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            "lastLogin:asc"  => query.OrderBy(u => u.LastLoginAt),
            "lastLogin:desc" => query.OrderByDescending(u => u.LastLoginAt),
            "createdAt:asc"  => query.OrderBy(u => u.CreatedAt),
            _                => query.OrderByDescending(u => u.CreatedAt),
        };

        var totalCount = await query.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var adminIds = adminRoleId != null
            ? (await db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId && userIds.Contains(ur.UserId))
                .Select(ur => ur.UserId)
                .ToListAsync()).ToHashSet()
            : new HashSet<string>();

        var totalUsers    = await db.Users.CountAsync();
        var adminCount    = adminRoleId != null ? await db.UserRoles.CountAsync(ur => ur.RoleId == adminRoleId) : 0;
        var suspendedCount = await db.Users.CountAsync(u => u.IsDeactivated);

        var dtos = users.Select(u => ToDto(u, adminIds.Contains(u.Id))).ToList();

        return Ok(new AdminUserListResponse(
            dtos, page, pageSize, totalCount,
            (int)Math.Ceiling((double)totalCount / pageSize),
            new AdminUserListMeta(totalUsers, adminCount, suspendedCount)));
    }

    [HttpPost("admins")]
    public async Task<ActionResult<CreateAdminResponse>> CreateAdmin(CreateAdminRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return Conflict(new { error = new { code = "EMAIL_TAKEN", message = "An account with this email already exists.", field = "email" } });

        var baseUsername = request.Email.Split('@')[0]
            .Replace(".", "_").Replace("+", "_").ToLower();
        var username = baseUsername;
        var suffix = 1;
        while (await userManager.FindByNameAsync(username) is not null)
            username = $"{baseUsername}{suffix++}";

        var user = new ApplicationUser
        {
            UserName = username,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
            return BadRequest(new { error = new { code = "VALIDATION", message = string.Join(", ", result.Errors.Select(e => e.Description)) } });

        await userManager.AddToRoleAsync(user, "Admin");
        await auditLog.LogAsync(AuditEvents.UserCreated,
            actorId: UserId, actorUsername: ActorUsername,
            targetId: user.Id, targetUsername: user.UserName, targetType: "user");

        return StatusCode(201, new CreateAdminResponse(ToDto(user, isAdmin: true), request.Email));
    }

    [HttpDelete("users")]
    public async Task<ActionResult<DeleteUsersResponse>> DeleteUsers(DeleteUsersRequest request)
    {
        if (request.Ids.Count == 0)
            return BadRequest(new { error = new { code = "VALIDATION", message = "No user IDs provided." } });

        if (request.Ids.Contains(UserId))
            return Conflict(new { error = new { code = "CANNOT_DELETE_SELF", message = "You cannot delete your own account." } });

        var deleted = new List<(string Id, string? Username)>();
        foreach (var id in request.Ids)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user is null) continue;
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded) deleted.Add((id, user.UserName));
        }

        if (deleted.Count == 1)
            await auditLog.LogAsync(AuditEvents.UserDeleted,
                actorId: UserId, actorUsername: ActorUsername,
                targetId: deleted[0].Id, targetUsername: deleted[0].Username, targetType: "user");
        else if (deleted.Count > 1)
            await auditLog.LogAsync(AuditEvents.UserDeletedBulk,
                actorId: UserId, actorUsername: ActorUsername,
                targetType: "user", affectedCount: deleted.Count);

        return Ok(new DeleteUsersResponse(deleted.Count, deleted.Select(d => d.Id).ToList()));
    }

    [HttpPost("users/suspend")]
    public async Task<IActionResult> SuspendUsers(BulkUserIdsRequest request)
    {
        var users = await db.Users.Where(u => request.Ids.Contains(u.Id)).ToListAsync();
        foreach (var user in users)
            user.IsDeactivated = true;

        await db.RefreshTokens
            .Where(t => request.Ids.Contains(t.UserId))
            .ExecuteDeleteAsync();

        await db.SaveChangesAsync();

        if (users.Count == 1)
            await auditLog.LogAsync(AuditEvents.UserSuspended,
                actorId: UserId, actorUsername: ActorUsername,
                targetId: users[0].Id, targetUsername: users[0].UserName, targetType: "user");
        else if (users.Count > 1)
            await auditLog.LogAsync(AuditEvents.UserSuspendedBulk,
                actorId: UserId, actorUsername: ActorUsername,
                targetType: "user", affectedCount: users.Count);

        return Ok(new { updated = users.Count });
    }

    [HttpPost("users/reactivate")]
    public async Task<IActionResult> ReactivateUsers(BulkUserIdsRequest request)
    {
        var users = await db.Users.Where(u => request.Ids.Contains(u.Id)).ToListAsync();
        foreach (var user in users)
            user.IsDeactivated = false;
        await db.SaveChangesAsync();

        if (users.Count == 1)
            await auditLog.LogAsync(AuditEvents.UserReactivated,
                actorId: UserId, actorUsername: ActorUsername,
                targetId: users[0].Id, targetUsername: users[0].UserName, targetType: "user");
        else if (users.Count > 1)
            await auditLog.LogAsync(AuditEvents.UserReactivatedBulk,
                actorId: UserId, actorUsername: ActorUsername,
                targetType: "user", affectedCount: users.Count);

        return Ok(new { updated = users.Count });
    }

    // ── Activity log ─────────────────────────────────────────────────────────

    [HttpGet("activity")]
    public async Task<ActionResult<AuditLogListResponse>> GetActivity(
        [FromQuery] string? type = null,
        [FromQuery] string? severity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(l => l.EventType.StartsWith(type));

        if (!string.IsNullOrEmpty(severity))
            query = query.Where(l => l.Severity == severity);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogEntryDto(
                l.Id, l.EventType, l.Severity, l.ActorId, l.ActorUsername,
                l.TargetId, l.TargetUsername, l.TargetType, l.AffectedCount, l.Details, l.CreatedAt))
            .ToListAsync();

        return Ok(new AuditLogListResponse(
            items, page, pageSize, totalCount,
            (int)Math.Ceiling((double)totalCount / pageSize)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AdminUserDto ToDto(ApplicationUser u, bool isAdmin) => new(
        u.Id,
        u.UserName ?? "",
        u.FirstName,
        u.LastName,
        $"{u.FirstName} {u.LastName}".Trim(),
        u.Email ?? "",
        isAdmin ? "admin" : "user",
        u.IsDeactivated ? "suspended" : "active",
        u.CreatedAt,
        u.LastLoginAt,
        u.AvatarId);
}
