using System.ComponentModel.DataAnnotations;
using Wander.Api.Domain;

namespace Wander.Api.Models.Users;

public record UpdateProfileRequest(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [MaxLength(50)] string? Pronouns,
    [MaxLength(500)] string? Bio,
    [MaxLength(10)] string? AvatarId);

public record UpdateSecurityRequest(
    [Required][EmailAddress][MaxLength(256)] string NewEmail,
    [Required][MinLength(3)][MaxLength(50)] string NewUsername,
    [Required][MaxLength(100)] string CurrentPassword,
    [MinLength(8)][MaxLength(100)] string? NewPassword);  // null = don't change

public record UpdatePrivacyRequest(
    Privacy EmailPrivacy,
    Privacy FirstNamePrivacy,
    Privacy LastNamePrivacy,
    Privacy PronounsPrivacy,
    Privacy BioPrivacy,
    Privacy FollowingCountPrivacy,
    Privacy FollowerCountPrivacy);

// Full self-view — includes all profile fields + privacy settings
public record MyProfileResponse(
    string Id, string Username, string Email,
    string? FirstName, string? LastName, string? Pronouns, string? Bio, string? AvatarId,
    Privacy EmailPrivacy, Privacy FirstNamePrivacy, Privacy LastNamePrivacy,
    Privacy PronounsPrivacy, Privacy BioPrivacy,
    Privacy FollowingCountPrivacy, Privacy FollowerCountPrivacy,
    DateTimeOffset CreatedAt);

// Public-facing view — fields already filtered by privacy + viewer relationship
// null means "hidden by privacy" (not "user has no value")
public record PublicProfileResponse(
    string Username,
    string? FirstName, string? LastName, string? Pronouns, string? Bio, string? AvatarId,
    string? Email, int? FollowingCount, int? FollowerCount,
    bool IsFollowing,
    List<PublicDeckSummary> PublicDecks,
    DateTimeOffset CreatedAt);

public record PublicDeckSummary(
    Guid Id, string Name, string? Description,
    string Format, int CardCount, DateTimeOffset UpdatedAt);