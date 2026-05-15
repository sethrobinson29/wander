using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Auth;
using Wander.Api.Services;

namespace Wander.Api.Controllers.Auth;

[ApiController]
[Route("auth")]
[EnableRateLimiting("auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    WanderDbContext db,
    TokenService tokenService,
    IAuditLogService auditLog) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await auditLog.LogAsync(AuditEvents.UserCreated,
            actorId: user.Id, actorUsername: user.UserName);

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        var passwordOk = user is not null && await userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordOk)
        {
            if (user is not null && await userManager.IsInRoleAsync(user, "Admin"))
                await auditLog.LogAsync(AuditEvents.AuthLoginFailed,
                    actorUsername: user.UserName, severity: AuditSeverity.Warning);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        if (user!.IsDeactivated)
            return Unauthorized(new { message = "Account suspended." });

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return Ok(await IssueTokensAsync(user));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
    {
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTimeOffset.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token." });

        // Token rotation — revoke the old token before issuing a new one
        stored.IsRevoked = true;
        await db.SaveChangesAsync();

        return Ok(await IssueTokensAsync(stored.User));
    }

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