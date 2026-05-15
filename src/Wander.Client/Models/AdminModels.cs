namespace Wander.Client.Models;

public record AdminUserDto(
    string Id,
    string Username,
    string? FirstName,
    string? LastName,
    string Name,
    string Email,
    string Role,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    string? AvatarId);

public record AdminUserListMeta(int TotalUsers, int AdminCount, int SuspendedCount);

public record AdminUserListResponse(
    List<AdminUserDto> Users,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    AdminUserListMeta Meta);

public record AdminCreateAdminRequest(
    string FirstName, string LastName, string Email, string TemporaryPassword);

public record AdminCreateAdminResponse(AdminUserDto User, string InviteSentTo);
public record AdminDeleteUsersResponse(int DeletedCount, List<string> DeletedIds);

public record AuditLogEntryDto(
    long Id,
    string EventType,
    string Severity,
    string? ActorId,
    string? ActorUsername,
    string? TargetId,
    string? TargetUsername,
    string? TargetType,
    int? AffectedCount,
    string? Details,
    DateTimeOffset CreatedAt);

public record AuditLogListResponse(
    List<AuditLogEntryDto> Entries,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

