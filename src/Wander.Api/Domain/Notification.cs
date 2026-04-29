using Wander.Api.Domain;

public class Notification
{
    public Guid Id { get; set; }
    public required string RecipientId { get; set; }
    public ApplicationUser Recipient { get; set; } = null!;
    public required string ActorId { get; set; }
    public ApplicationUser Actor { get; set; } = null!;
    public NotificationType Type { get; set; }
    public Guid? DeckId { get; set; }
    public Deck? Deck { get; set; }
    public string? DeckName { get; set; }    // snapshot — deck may be deleted later
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
