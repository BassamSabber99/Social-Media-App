namespace SocialMediaApp.Domain.Entities;

public class UserFollower
{
    public Guid FollowerId { get; set; }
    public Guid FollowedId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? Follower { get; set; }
    public User? Followed { get; set; }
}

