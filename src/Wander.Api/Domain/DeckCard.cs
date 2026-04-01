namespace Wander.Api.Domain;

public class DeckCard
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Deck Deck { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public int Quantity { get; set; }
    public bool IsCommander { get; set; }
    public bool IsSideboard { get; set; }
}