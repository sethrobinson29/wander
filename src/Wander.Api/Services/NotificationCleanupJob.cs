using Microsoft.EntityFrameworkCore;
using Quartz;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Services;

[DisallowConcurrentExecution]
public class NotificationCleanupJob(
    WanderDbContext db,
    ILogger<NotificationCleanupJob> logger,
    IAuditLogService auditLog) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTimeOffset.UtcNow;

        await auditLog.LogAsync(AuditEvents.JobNotifyCleanupStarted,
            targetId: "notify-cleanup", targetType: "job");

        var deleted = await db.Notifications
            .Where(n => (n.IsRead  && now - n.CreatedAt > TimeSpan.FromDays(30))
                     || (!n.IsRead && now - n.CreatedAt > TimeSpan.FromDays(90)))
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Notification cleanup removed {Count} notifications.", deleted);

        await auditLog.LogAsync(AuditEvents.JobNotifyCleanupCompleted,
            targetId: "notify-cleanup", targetType: "job", affectedCount: deleted);
    }
}
