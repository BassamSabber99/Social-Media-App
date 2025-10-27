using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface ILikeRepository
{
    Task<int> CountForPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<bool> IsPostLikedByUserAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<Like?> GetAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Like like, CancellationToken cancellationToken = default);
    void Remove(Like like);
}

