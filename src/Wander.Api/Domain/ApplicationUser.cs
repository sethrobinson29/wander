using Microsoft.AspNetCore.Identity;

namespace Wander.Api.Domain;

public class ApplicationUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; }
}