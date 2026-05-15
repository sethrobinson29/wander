using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Services;

public class AuditLogService(WanderDbContext db) : IAuditLogService
{
    public async Task LogAsync(
        string eventType,
        string? actorId = null,
        string? actorUsername = null,
        string? targetId = null,
        string? targetUsername = null,
        string? targetType = null,
        int? affectedCount = null,
        AuditSeverity severity = AuditSeverity.Info,
        string? details = null)
    {
        db.AuditLogs.Add(new AdminAuditLog
        {
            EventType = eventType,
            Severity = severity.ToString().ToLower(),
            ActorId = actorId,
            ActorUsername = actorUsername,
            TargetId = targetId,
            TargetUsername = targetUsername,
            TargetType = targetType,
            AffectedCount = affectedCount,
            Details = details,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
