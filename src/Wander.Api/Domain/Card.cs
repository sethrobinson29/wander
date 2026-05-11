using NpgsqlTypes;

namespace Wander.Api.Domain;

public class Card
{
    public Guid Id { get; set; }
    public required string ScryfallId { get; set; }
    public required string Name { get; set; }
    public string? ManaCost { get; set; }
    public decimal Cmc { get; set; }
    public required string TypeLine { get; set; }
    public string? OracleText { get; set; }
    public string? BackFaceManaCost { get; set; }
    public string? BackFaceTypeLine { get; set; }
    public string? BackFaceOracleText { get; set; }
    public List<string> Colors { get; set; } = [];
    public List<string> ColorIdentity { get; set; } = [];
    public Dictionary<string, string> Legalities { get; set; } = [];
    public NpgsqlTsVector? NameSearchVector { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<CardPrinting> Printings { get; set; } = [];
}
