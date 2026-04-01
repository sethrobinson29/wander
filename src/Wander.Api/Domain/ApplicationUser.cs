using Microsoft.AspNetCore.Identity;

namespace Wander.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Deck> Decks { get; set; } = [];
}