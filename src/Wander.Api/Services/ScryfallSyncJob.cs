using Microsoft.EntityFrameworkCore;
using Quartz;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Infrastructure.Scryfall;

namespace Wander.Api.Services;

[DisallowConcurrentExecution]
public class ScryfallSyncJob(
    ScryfallBulkDataService syncService,
    WanderDbContext db,
    ILogger<ScryfallSyncJob> logger,
    AuditLogService auditLog) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var hasCards = await db.Cards.AnyAsync(ct);
        if (hasCards)
        {
            var mostRecent = await db.Cards.MaxAsync(c => c.UpdatedAt, ct);
            if (DateTimeOffset.UtcNow - mostRecent < TimeSpan.FromDays(7))
            {
                logger.LogInformation("Skipping Scryfall sync — data is {Age:0.1} days old.", (DateTimeOffset.UtcNow - mostRecent).TotalDays);
                await auditLog.LogAsync("job.sync.skipped", targetId: "scryfall", targetType: "job");
                return;
            }
        }

        await syncService.SyncAsync(ct);
    }
}
