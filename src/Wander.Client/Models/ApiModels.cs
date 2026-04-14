using static MudBlazor.CategoryTypes;

namespace Wander.Client.Models;

// ── Auth ─────────────────────────────────────────────────────────────────────

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

// ── Users ────────────────────────────────────────────────────────────────────

public enum Privacy { Public, Restricted, Private }

public record MyProfileResponse(
    string Id, string Username, string Email,
    string? FirstName, string? LastName, string? Pronouns, string? Bio, string? AvatarId,
    Privacy EmailPrivacy, Privacy FirstNamePrivacy, Privacy LastNamePrivacy,
    Privacy PronounsPrivacy, Privacy BioPrivacy,
    Privacy FollowingCountPrivacy, Privacy FollowerCountPrivacy,
    DateTimeOffset CreatedAt);

public record PublicProfileResponse(
    string Username, string? FirstName, string? LastName,
    string? Pronouns, string? Bio, string? AvatarId,
    string? Email, int? FollowingCount, int? FollowerCount,
    bool IsFollowing,
    List<PublicDeckSummary> PublicDecks, DateTimeOffset CreatedAt);

public record PublicDeckSummary(
    Guid Id, string Name, string? Description,
    string Format, int CardCount, DateTimeOffset UpdatedAt);

public record UpdateProfileRequest(
    string? FirstName, string? LastName, string? Pronouns, string? Bio, string? AvatarId);

public record UpdateSecurityRequest(
    string NewEmail, string NewUsername,
    string CurrentPassword, string? NewPassword);

public record UpdatePrivacyRequest(
    Privacy EmailPrivacy, Privacy FirstNamePrivacy, Privacy LastNamePrivacy,
    Privacy PronounsPrivacy, Privacy BioPrivacy,
    Privacy FollowingCountPrivacy, Privacy FollowerCountPrivacy);

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
    Guid? DefaultPrintingId,
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
    int LikeCount,
    bool IsLikedByCurrentUser,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record DeckCardDetail(
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

// ── Requests ─────────────────────────────────────────────────────────────────

public record CreateDeckRequest(string Name, string? Description, Format Format, Visibility Visibility);

public record UpdateDeckRequest(string Name, string? Description, string? Primer, Format Format, Visibility Visibility);

public record DeckCardRequest(Guid CardId, Guid? PrintingId, int Quantity, bool IsCommander, bool IsSideboard);

public record BulkImportRequest(string Decklist);
