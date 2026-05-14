using System.ComponentModel.DataAnnotations;

namespace Wander.Api.Models.Admin;

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

public record CreateAdminRequest(
    [Required][MaxLength(100)] string FirstName,
    [Required][MaxLength(100)] string LastName,
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)][MaxLength(100)] string TemporaryPassword);

public record CreateAdminResponse(AdminUserDto User, string InviteSentTo);

public record DeleteUsersRequest([Required] List<string> Ids);
public record DeleteUsersResponse(int DeletedCount, List<string> DeletedIds);

public record BulkUserIdsRequest([Required] List<string> Ids);
