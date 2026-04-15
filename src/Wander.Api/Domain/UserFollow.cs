namespace Wander.Api.Domain;

public class UserFollow
{
    public required string FollowerId { get; set; }
    public ApplicationUser Follower { get; set; } = null!;
    public required string FolloweeId { get; set; }
    public ApplicationUser Followee { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
