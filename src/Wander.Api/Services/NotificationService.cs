using Microsoft.AspNetCore.SignalR;
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