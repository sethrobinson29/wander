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

    public Privacy FirstNamePrivacy { get; set; } = Privacy.Public;
    public Privacy LastNamePrivacy { get; set; } = Privacy.Public;
    public Privacy PronounsPrivacy { get; set; } = Privacy.Public;
    public Privacy BioPrivacy { get; set; } = Privacy.Public;
    public Privacy EmailPrivacy { get; set; } = Privacy.Private;
    public Privacy FollowingCountPrivacy { get; set; } = Privacy.Public;
    public Privacy FollowerCountPrivacy { get; set; } = Privacy.Public;
    public ICollection<Deck> Decks { get; set; } = [];
    public ICollection<UserFollow> Following { get; set; } = [];
    public ICollection<UserFollow> Followers { get; set; } = [];
    public ICollection<DeckLike> LikedDecks { get; set; } = [];
    public Privacy ActivityPrivacy { get; set; } = Privacy.Public;
    public ICollection<UserActivity> Activities { get; set; } = [];
    public ICollection<Notification> ReceivedNotifications { get; set; } = [];
}