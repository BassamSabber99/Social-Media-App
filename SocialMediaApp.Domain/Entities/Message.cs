using SocialMediaApp.Domain.Enums;

namespace SocialMediaApp.Domain.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Chat? Chat { get; set; }
    public User? Sender { get; set; }
}

