namespace Wander.Api.Domain
{
    public class DeckLike
    {
        public required string UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public Guid DeckId { get; set; }
        public Deck Deck { get; set; } = null!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
