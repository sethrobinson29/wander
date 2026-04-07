namespace Wander.Api.Domain;

public class CardPrinting
{
    public Guid Id { get; set; }
    public required string ScryfallId { get; set; }
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public required string SetCode { get; set; }
    public required string CollectorNumber { get; set; }
    public string? ImageUriNormal { get; set; }
    public string? ImageUriSmall { get; set; }
    public string? ImageUriArtCrop { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
