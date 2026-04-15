namespace Wander.Api.Domain;

public class DeckComment
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public required string AuthorId { get; set; }
    public ApplicationUser Author { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }       // null = top-level comment
    public DeckComment? ParentComment { get; set; }
    public ICollection<DeckComment> Replies { get; set; } = [];
    public required string Body { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }              // soft delete — preserves thread structure
}

