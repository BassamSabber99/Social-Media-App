namespace SocialMediaApp.Domain.Entities;

public class Chat
{
    public Guid Id { get; set; }
    public Guid User1Id { get; set; }
    public Guid User2Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;

    public User? User1 { get; set; }
    public User? User2 { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

