using Wander.Api.Domain;

namespace Wander.Api.Models.Decks;

public record CreateDeckRequest(
    string Name,
    string? Description,
    Format Format,
    Visibility Visibility);

public record UpdateDeckRequest(
    string Name,
    string? Description,
    string? Primer,
    Format Format,
    Visibility Visibility);

public record DeckCardRequest(
    Guid CardId,
    int Quantity,
    bool IsCommander,
    bool IsSideboard);

public record DeckSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    Format Format,
    Visibility Visibility,
    string OwnerUsername,
    int CardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DeckDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Primer,
    Format Format,
    Visibility Visibility,
    string OwnerUsername,
    List<DeckCardResponse> Cards,
    List<string> DeckErrors,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DeckCardResponse(
    Guid Id,
    Guid CardId,
    string CardName,
    string? ManaCost,
    decimal Cmc,
    string TypeLine,
    List<string> ColorIdentity,
    string? ImageUriNormal,
    string? ImageUriSmall,
    int Quantity,
    bool IsCommander,
    bool IsSideboard,
    List<string> Errors);

public record BulkImportRequest(string Decklist);