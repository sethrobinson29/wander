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
    Dictionary<string, string> Legalities,
    string? BackFaceManaCost,
    string? BackFaceTypeLine,
    string? BackFaceOracleText,
    string? BackImageUriNormal);

public record CardPrintingResponse(
    Guid Id,
    string SetCode,
    string CollectorNumber,
    string? ImageUriNormal,
    string? ImageUriSmall,
    string? ImageUriArtCrop);
