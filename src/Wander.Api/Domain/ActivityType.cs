namespace Wander.Api.Domain;
public enum ActivityType
{
    LikedDeck,
    CommentedOnDeck,
    FollowedUser,
    MadeDeckPublic
}

public class UserActivity
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public ActivityType Type { get; set; }
    public string? TargetId { get; set; }   // DeckId (as string) or followed UserId
    public string? TargetName { get; set; }   // Deck name or username — snapshot at time of event
    public DateTimeOffset CreatedAt { get; set; }
}
