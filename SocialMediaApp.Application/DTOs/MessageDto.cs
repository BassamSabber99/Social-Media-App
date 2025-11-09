using SocialMediaApp.Domain.Enums;

namespace SocialMediaApp.Application.DTOs;

public sealed class MessageDto
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderUserName { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

