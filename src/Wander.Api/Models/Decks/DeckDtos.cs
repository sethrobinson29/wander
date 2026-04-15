using System.ComponentModel.DataAnnotations;
using Wander.Api.Domain;

namespace Wander.Api.Models.Decks;

public record CreateDeckRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description,
    Format Format,
    Visibility Visibility);

public record UpdateDeckRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description,
    [MaxLength(50000)] string? Primer,
    Format Format,
    Visibility Visibility);

public record DeckCardRequest(
    Guid CardId,
    Guid? PrintingId,
    [Range(1, 99)] int Quantity,
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
    string OwnerId,
    string OwnerUsername,
    List<DeckCardResponse> Cards,
    List<string> DeckErrors,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int LikeCount,
    bool IsLikedByCurrentUser);

public record DeckCardResponse(
    Guid Id,
    Guid CardId,
    Guid? PrintingId,
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

public record BulkImportRequest([Required][MaxLength(200_000)] string Decklist);
