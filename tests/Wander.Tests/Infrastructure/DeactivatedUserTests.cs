using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Wander.Api.Controllers;
using Wander.Api.Controllers.Auth;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Admin;
using Wander.Api.Models.Auth;
using Wander.Api.Services;
using Wander.Tests.Helpers;

namespace Wander.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class DeactivatedUserTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev";

    private ServiceProvider _provider = null!;
    private WanderDbContext _db = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private TokenService _tokenService = null!;
    private IAuditLogService _auditLog = null!;
    private readonly List<string> _createdUserIds = [];

    public async Task InitializeAsync()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddDbContext<WanderDbContext>(opt => opt.UseNpgsql(dataSource));
        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<WanderDbContext>();

        _provider = services.BuildServiceProvider();
        _db = _provider.GetRequiredService<WanderDbContext>();
        _userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('x', 64),
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();
        _tokenService = new TokenService(config);
        _auditLog = new AuditLogService(_db);
    }

    public async Task DisposeAsync()
    {
        foreach (var id in _createdUserIds)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is not null) await _userManager.DeleteAsync(user);
        }
        await _provider.DisposeAsync();
    }

    private async Task<ApplicationUser> CreateUserAsync(string password = "Test@1234!")
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var user = new ApplicationUser
        {
            UserName = $"testuser_{id}",
            Email = $"test_{id}@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var result = await _userManager.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        _createdUserIds.Add(user.Id);
        return user;
    }

    private AuthController MakeAuthController() =>
        new(_userManager, _db, _tokenService, _auditLog);

    private UserController MakeUserController(string userId) =>
        new(_userManager, _db, _tokenService,
            new ActivityService(_db),
            new NotificationService(_db, NullHubContext<Wander.Api.Hubs.NotificationHub>.Instance),
            _auditLog)
        {
            ControllerContext = ControllerContextFor(userId),
        };

    private AdminController MakeAdminController(string actorId, string actorUsername)
    {
        var controller = new AdminController(_userManager, _db, _auditLog,
            _provider.GetRequiredService<IServiceScopeFactory>())
        {
            ControllerContext = ControllerContextFor(actorId, actorUsername),
        };
        return controller;
    }

    private static ControllerContext ControllerContextFor(string userId, string? username = null) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username ?? userId),
            ]))
        }
    };

    // ── Login blocked ────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Returns401_WhenUserIsDeactivated()
    {
        var user = await CreateUserAsync();
        user.IsDeactivated = true;
        await _userManager.UpdateAsync(user);

        var controller = MakeAuthController();
        var result = await controller.Login(new LoginRequest(user.Email!, "Test@1234!"));

        var status = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var body = status.Value?.ToString();
        Assert.Contains("suspended", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Self-deactivation ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMyAccount_SetsIsDeactivated()
    {
        var user = await CreateUserAsync();
        var controller = MakeUserController(user.Id);

        var result = await controller.DeleteMyAccount();

        Assert.IsType<NoContentResult>(result);
        var updated = await _userManager.FindByIdAsync(user.Id);
        Assert.True(updated!.IsDeactivated);
    }

    [Fact]
    public async Task DeleteMyAccount_RevokesAllRefreshTokens()
    {
        var user = await CreateUserAsync();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "tok1", UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "tok2", UserId = user.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await _db.SaveChangesAsync();

        var controller = MakeUserController(user.Id);
        await controller.DeleteMyAccount();

        var remaining = await _db.RefreshTokens.Where(t => t.UserId == user.Id).CountAsync();
        Assert.Equal(0, remaining);
    }

    // ── Admin suspend / reactivate ───────────────────────────────────────────

    [Fact]
    public async Task AdminSuspend_SetsIsDeactivated()
    {
        var actor = await CreateUserAsync();
        var target = await CreateUserAsync();
        var controller = MakeAdminController(actor.Id, actor.UserName!);

        var result = await controller.SuspendUsers(new BulkUserIdsRequest([target.Id]));

        Assert.IsType<OkObjectResult>(result);
        var updated = await _userManager.FindByIdAsync(target.Id);
        Assert.True(updated!.IsDeactivated);
    }

    [Fact]
    public async Task AdminSuspend_RevokesRefreshTokens()
    {
        var actor = await CreateUserAsync();
        var target = await CreateUserAsync();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), Token = "tok_target", UserId = target.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });
        await _db.SaveChangesAsync();

        var controller = MakeAdminController(actor.Id, actor.UserName!);
        await controller.SuspendUsers(new BulkUserIdsRequest([target.Id]));

        var remaining = await _db.RefreshTokens.Where(t => t.UserId == target.Id).CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task AdminReactivate_ClearsIsDeactivated()
    {
        var actor = await CreateUserAsync();
        var target = await CreateUserAsync();
        target.IsDeactivated = true;
        await _userManager.UpdateAsync(target);

        var controller = MakeAdminController(actor.Id, actor.UserName!);
        var result = await controller.ReactivateUsers(new BulkUserIdsRequest([target.Id]));

        Assert.IsType<OkObjectResult>(result);
        var updated = await _userManager.FindByIdAsync(target.Id);
        Assert.False(updated!.IsDeactivated);
    }
}
