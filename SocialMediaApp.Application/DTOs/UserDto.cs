namespace SocialMediaApp.Application.DTOs;

public sealed record UserDto
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required string Bio { get; init; }
    public string? ProfileImageUrl { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public int FollowersCount { get; init; }
    public int FollowingCount { get; init; }
    public bool IsFollowedByRequester { get; init; }
}

