using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IUserFollowerRepository
{
    Task<UserFollower?> GetAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetFollowersAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetFollowingAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountFollowersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> CountFollowingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserFollower follow, CancellationToken cancellationToken = default);
    void Remove(UserFollower follow);
}

