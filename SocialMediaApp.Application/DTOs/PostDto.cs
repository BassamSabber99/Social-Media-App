namespace SocialMediaApp.Application.DTOs;

public sealed class PostDto
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorUserName { get; init; } = string.Empty;
    public string AuthorDisplayName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int CommentCount { get; init; }
    public int LikeCount { get; init; }
    public bool IsLikedByRequester { get; init; }
    public bool IsAuthorFollowedByRequester { get; init; }
}

