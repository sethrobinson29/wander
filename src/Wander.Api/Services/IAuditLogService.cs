using Wander.Api.Domain;

namespace Wander.Api.Services;

public interface IAuditLogService
{
    Task LogAsync(
        string eventType,
        string? actorId = null,
        string? actorUsername = null,
        string? targetId = null,
        string? targetUsername = null,
        string? targetType = null,
        int? affectedCount = null,
        AuditSeverity severity = AuditSeverity.Info,
        string? details = null);
}
