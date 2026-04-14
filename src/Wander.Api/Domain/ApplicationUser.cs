using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Wander.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; }
    // Profile fields
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Pronouns { get; set; }
    public string? Bio { get; set; }
    [MaxLength(10)]
    public string? AvatarId { get; set; }

    // Per-field privacy (all default Public so existing accounts stay visible)
    public Privacy FirstNamePrivacy { get; set; } = Privacy.Public;
    public Privacy LastNamePrivacy { get; set; } = Privacy.Public;
    public Privacy PronounsPrivacy { get; set; } = Privacy.Public;
    public Privacy BioPrivacy { get; set; } = Privacy.Public;
    public Privacy EmailPrivacy { get; set; } = Privacy.Public;
    public Privacy FollowingCountPrivacy { get; set; } = Privacy.Public;
    public Privacy FollowerCountPrivacy { get; set; } = Privacy.Public;
    public ICollection<Deck> Decks { get; set; } = [];
    public ICollection<UserFollow> Following { get; set; } = []; // UserFollows where this user is the follower
    public ICollection<UserFollow> Followers { get; set; } = []; // UserFollows where this user is the followee
}