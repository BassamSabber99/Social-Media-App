using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Post?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Post>> GetFeedAsync(Guid requesterId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountFeedAsync(Guid requesterId, CancellationToken cancellationToken = default);
    Task AddAsync(Post post, CancellationToken cancellationToken = default);
    void Update(Post post);
    void Remove(Post post);
}

