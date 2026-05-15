using Wander.Api.Domain;
using Wander.Api.Services;

namespace Wander.Tests.Helpers;

public sealed class NullAuditLogService : IAuditLogService
{
    public static readonly NullAuditLogService Instance = new();

    public Task LogAsync(
        string eventType,
        string? actorId = null,
        string? actorUsername = null,
        string? targetId = null,
        string? targetUsername = null,
        string? targetType = null,
        int? affectedCount = null,
        AuditSeverity severity = AuditSeverity.Info,
        string? details = null) => Task.CompletedTask;
}
