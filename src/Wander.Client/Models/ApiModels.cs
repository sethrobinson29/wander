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
    Privacy ActivityPrivacy,
    DateTimeOffset CreatedAt);

public record PublicProfileResponse(
    string Username, string? FirstName, string? LastName,
    string? Pronouns, string? Bio, string? AvatarId,
    string? Email, int? FollowingCount, int? FollowerCount,
    bool IsFollowing,
    Privacy ActivityPrivacy,
    List<PublicDeckSummary> PublicDecks, DateTimeOffset CreatedAt);

public record PublicDeckSummary(
    Guid Id, string Name, string? Description,
    string Format, int CardCount, List<string> ColorIdentity, DateTimeOffset UpdatedAt);

public record UpdateProfileRequest(
    string? FirstName, string? LastName, string? Pronouns, string? Bio, string? AvatarId);

public record UpdateSecurityRequest(
    string NewEmail, string NewUsername,
    string CurrentPassword, string? NewPassword);

public record UpdatePrivacyRequest(
    Privacy EmailPrivacy, Privacy FirstNamePrivacy, Privacy LastNamePrivacy,
    Privacy PronounsPrivacy, Privacy BioPrivacy,
    Privacy FollowingCountPrivacy, Privacy FollowerCountPrivacy,
    Privacy ActivityPrivacy);

public record ActivityItem(string Type, string? TargetId, string? TargetName, DateTimeOffset CreatedAt);
public record ActivityPageResponse(List<ActivityItem> Items, int Total);

public record UserSearchResult(string Username, string? AvatarId, int DeckCount);

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
    string? ImageUriArtCrop,
    Dictionary<string, string> Legalities);

public record CardPrintingInfo(
    Guid Id,
    string SetCode,
    string CollectorNumber,
    string? ImageUriNormal,
    string? ImageUriSmall,
    string? ImageUriArtCrop);

// ── Decks ────────────────────────────────────────────────────────────────────

public enum Format { Standard, Pioneer, Modern, Legacy, Vintage, Commander, Pauper, Explorer, Historic, Timeless }
public enum Visibility { Public, Private, Unlisted }
public enum DeckGroupBy { Type, ManaValue, Color, ColorIdentity, None }
public enum DeckSortBy  { ManaValue, Name }

public record DeckSummary(
    Guid Id, string Name, string? Description, Format Format,
    string? CoverImageUri,
    double? CoverCropLeft, double? CoverCropTop,
    double? CoverCropWidth, double? CoverCropHeight,
    List<string> ColorIdentity, Visibility Visibility,
    string OwnerUsername, int CardCount,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record DeckDetail(
    Guid Id, string Name, string? Description, string? Primer,
    Format Format, string? CoverImageUri,
    double? CoverCropLeft, double? CoverCropTop,
    double? CoverCropWidth, double? CoverCropHeight,
    Visibility Visibility, string OwnerId, string OwnerUsername,
    List<DeckCardDetail> Cards, List<string> DeckErrors, int LikeCount, bool IsLikedByCurrentUser,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record DeckCardDetail(
    Guid Id,
    Guid CardId,
    Guid? PrintingId,
    string CardName,
    string? ManaCost,
    decimal Cmc,
    string TypeLine,
    string? OracleText,
    string? FlavorText,
    Dictionary<string, string> Legalities,
    List<string> ColorIdentity,
    string? ImageUriNormal,
    string? ImageUriSmall,
    string? ImageUriArtCrop,
    Guid? ImagePrintingId,
    int Quantity,
    bool IsCommander,
    bool IsSideboard,
    List<string> Errors);

public record CommentResponse(
    Guid Id,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarId,
    string Body,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<CommentResponse> Replies);

public record PostCommentRequest(string Body);

// ── Requests ─────────────────────────────────────────────────────────────────

public record CreateDeckRequest(string Name, string? Description, Format Format, Visibility Visibility);

public record UpdateDeckRequest(string Name, string? Description, string? Primer, Format Format, Visibility Visibility);

public record DeckCardRequest(Guid CardId, Guid? PrintingId, int Quantity, bool IsCommander, bool IsSideboard);

public record BulkImportRequest(string Decklist);

// ── Notifications ────────────────────────────────────────────────────────────

public record NotificationItem(
    Guid Id,
    string Type,
    string ActorId,
    string? ActorUsername,
    Guid? DeckId,
    string? DeckName,
    bool IsRead,
    DateTimeOffset CreatedAt);

public record NotificationListResponse(int UnreadCount, List<NotificationItem> Items);
