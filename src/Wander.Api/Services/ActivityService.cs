using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Services;

public class ActivityService(WanderDbContext db)
{
    public void Record(string userId, ActivityType type,
        string? targetId = null, string? targetName = null)
    {
        db.UserActivities.Add(new UserActivity
        {
            UserId = userId,
            Type = type,
            TargetId = targetId,
            TargetName = targetName,
            CreatedAt = DateTimeOffset.UtcNow
        });
        // Caller is responsible for calling SaveChangesAsync —
        // this batches with whatever else the calling endpoint is saving.
    }

    // Exposed for unit testing
    internal static bool IsMadePublic(Visibility previous, Visibility next) =>
        previous != Visibility.Public && next == Visibility.Public;
}