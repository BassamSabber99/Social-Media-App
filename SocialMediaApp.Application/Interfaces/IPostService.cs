using SocialMediaApp.Application.DTOs;

namespace SocialMediaApp.Application.Interfaces;

public interface IPostService
{
    Task<PostDto> CreatePostAsync(Guid authorId, string content, string? imageUrl, CancellationToken cancellationToken = default);
    Task<PostDto?> GetPostAsync(Guid postId, Guid requesterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostDto>> GetFeedAsync(Guid requesterId, int skip, int take, CancellationToken cancellationToken = default);
    Task LikePostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task UnlikePostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountFeedAsync(Guid requesterId, CancellationToken cancellationToken = default);
}

