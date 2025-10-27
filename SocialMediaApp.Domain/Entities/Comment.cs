namespace SocialMediaApp.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public Post? Post { get; set; }
    public User? Author { get; set; }
}

