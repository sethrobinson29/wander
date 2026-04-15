namespace Wander.Api.Domain;

public class Deck
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Primer { get; set; }          // Markdown string
    public Format Format { get; set; }
    public Visibility Visibility { get; set; }
    public required string OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    public List<DeckCard> Cards { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<DeckLike> Likes { get; set; } = [];
    public ICollection<DeckComment> Comments { get; set; } = [];
}