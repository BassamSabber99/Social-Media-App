namespace SocialMediaApp.Application.DTOs;

public sealed class ChatDto
{
    public Guid Id { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserDisplayName { get; set; } = string.Empty;
    public string OtherUserProfileImageUrl { get; set; } = string.Empty;
    public DateTime LastMessageAtUtc { get; set; }
    public string? LastMessageContent { get; set; }
    public int UnreadCount { get; set; }
}

