namespace SocialMediaApp.Application.DTOs;

public sealed class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

