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
    string? ImageUriArtCrop,
    Dictionary<string, string> Legalities);

public record CardPrintingResponse(
    Guid Id,
    string SetCode,
    string CollectorNumber,
    string? ImageUriNormal,
    string? ImageUriSmall,
    string? ImageUriArtCrop);
