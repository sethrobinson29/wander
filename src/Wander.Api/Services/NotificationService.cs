using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Hubs;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Services;

public class NotificationService(WanderDbContext db, IHubContext<NotificationHub> hub)
{
    /// <summary>
    /// Creates a persistent notification record and pushes a real-time event to the recipient.
    /// No-op when recipientId == actorId.
    /// </summary>
    public async Task NotifyAsync(string recipientId, string actorId,
        NotificationType type, Guid? deckId = null, string? deckName = null,
        string? actorUsername = null)
    {
        if (!ShouldNotify(recipientId, actorId)) return;

        var notification = new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            DeckId = deckId,
            DeckName = deckName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Push to the recipient's SignalR group
        await hub.Clients
            .Group($"user-{recipientId}")
            .SendAsync("ReceiveNotification", new NotificationDto(
                notification.Id,
                type.ToString(),
                actorId,
                actorUsername,
                deckId,
                deckName,
                notification.CreatedAt));
    }

    /// <summary>
    /// Notifies all followers of actorId that a deck was made public.
    /// Skips followers who already received this notification for the same deck within 24 hours
    /// to avoid spam when visibility is toggled repeatedly.
    /// </summary>
    public async Task NotifyFollowersAsync(string actorId, string actorUsername,
        Guid deckId, string deckName)
    {
        var followerIds = await db.Follows
            .Where(f => f.FolloweeId == actorId)
            .Select(f => f.FollowerId)
            .ToListAsync();

        if (followerIds.Count == 0) return;

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var alreadyNotified = await db.Notifications
            .Where(n => n.ActorId == actorId &&
                        n.DeckId == deckId &&
                        n.Type == NotificationType.DeckMadePublic &&
                        n.CreatedAt >= cutoff)
            .Select(n => n.RecipientId)
            .ToHashSetAsync();

        foreach (var followerId in followerIds)
        {
            if (alreadyNotified.Contains(followerId)) continue;
            await NotifyAsync(followerId, actorId, NotificationType.DeckMadePublic,
                deckId, deckName, actorUsername);
        }
    }

    // Extracted for unit testing
    internal static bool ShouldNotify(string recipientId, string actorId) =>
        recipientId != actorId;
}

public record NotificationDto(
    Guid Id,
    string Type,
    string ActorId,
    string? ActorUsername,
    Guid? DeckId,
    string? DeckName,
    DateTimeOffset CreatedAt);