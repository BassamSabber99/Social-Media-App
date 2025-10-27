namespace SocialMediaApp.Application.DTOs;

public sealed class SendMessageRequest
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}

