namespace SocialMediaApp.Domain.Entities;

public class Post
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public User? Author { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
}

