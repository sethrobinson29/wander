using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Services;

namespace Wander.Tests;

public class AuditLogServiceTests
{
    private static WanderDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WanderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task LogAsync_WritesEntryToDatabase()
    {
        await using var db = CreateDb();
        var svc = new AuditLogService(db);

        await svc.LogAsync(AuditEvents.UserSuspended,
            actorId: "actor-1", actorUsername: "admin",
            targetId: "target-1", targetUsername: "bob", targetType: "user");

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal(AuditEvents.UserSuspended, entry.EventType);
        Assert.Equal("actor-1", entry.ActorId);
        Assert.Equal("admin", entry.ActorUsername);
        Assert.Equal("target-1", entry.TargetId);
        Assert.Equal("bob", entry.TargetUsername);
        Assert.Equal("user", entry.TargetType);
    }

    [Fact]
    public async Task LogAsync_SeverityStoredAsLowercase()
    {
        await using var db = CreateDb();
        var svc = new AuditLogService(db);

        await svc.LogAsync(AuditEvents.AuthLoginFailed, severity: AuditSeverity.Warning);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal("warning", entry.Severity);
    }

    [Fact]
    public async Task LogAsync_DefaultSeverityIsInfo()
    {
        await using var db = CreateDb();
        var svc = new AuditLogService(db);

        await svc.LogAsync(AuditEvents.UserCreated);

        var entry = await db.AuditLogs.SingleAsync();
        Assert.Equal("info", entry.Severity);
    }
}
