namespace SocialMediaApp.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Chat? Chat { get; set; }
    public User? Sender { get; set; }
}

