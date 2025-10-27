namespace SocialMediaApp.Application.DTOs;

public sealed record CommentDto
{
    public required Guid Id { get; init; }
    public required Guid PostId { get; init; }
    public required Guid AuthorId { get; init; }
    public required string AuthorUserName { get; init; }
    public required string AuthorDisplayName { get; init; }
    public required string Content { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

