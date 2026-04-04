namespace Wander.Client.Models;

// ── Auth ─────────────────────────────────────────────────────────────────────

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

// ── Cards ────────────────────────────────────────────────────────────────────

public record CardSearchResult(
    Guid Id,
    string Name,
    string? ManaCost,
    decimal Cmc,
    string TypeLine,
    string? OracleText,
    List<string> Colors,
    List<string> ColorIdentity,
    string? ImageUriNormal,
    string? ImageUriSmall,
    Dictionary<string, string> Legalities);

// ── Decks ────────────────────────────────────────────────────────────────────

public enum Format { Standard, Pioneer, Modern, Legacy, Vintage, Commander, Pauper, Explorer, Historic, Timeless }
public enum Visibility { Public, Private, Unlisted }

public record DeckSummary(
    Guid Id,
    string Name,
    string? Description,
    Format Format,
    Visibility Visibility,
    string OwnerUsername,
    int CardCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DeckDetail(
    Guid Id,
    string Name,
    string? Description,
    string? Primer,
    Format Format,
    Visibility Visibility,
    string OwnerUsername,
    List<DeckCardDetail> Cards,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DeckCardDetail(
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

// ── Requests ─────────────────────────────────────────────────────────────────

public record CreateDeckRequest(string Name, string? Description, Format Format, Visibility Visibility);

public record UpdateDeckRequest(string Name, string? Description, string? Primer, Format Format, Visibility Visibility);

public record DeckCardRequest(Guid CardId, int Quantity, bool IsCommander, bool IsSideboard);

public record BulkImportRequest(string Decklist);