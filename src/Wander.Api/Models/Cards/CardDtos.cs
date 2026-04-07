namespace Wander.Api.Models.Cards;

public record CardSearchResponse(
    Guid Id,
    string Name,
    string? ManaCost,
    decimal Cmc,
    string TypeLine,
    string? OracleText,
    List<string> Colors,
    List<string> ColorIdentity,
    Guid? DefaultPrintingId,
    string? ImageUriNormal,
    string? ImageUriSmall,
    Dictionary<string, string> Legalities);
