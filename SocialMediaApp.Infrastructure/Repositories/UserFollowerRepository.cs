using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public class UserFollowerRepository(AppDbContext dbContext) : IUserFollowerRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<UserFollower?> GetAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == followerId && f.FollowedId == followedId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsFollowingAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == followerId && f.FollowedId == followedId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetFollowersAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .Where(f => f.FollowedId == userId)
            .Include(f => f.Follower)
            .Select(f => f.Follower!)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<User>> GetFollowingAsync(Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Followed)
            .Select(f => f.Followed!)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountFollowersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .CountAsync(f => f.FollowedId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountFollowingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserFollowers
            .AsNoTracking()
            .CountAsync(f => f.FollowerId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(UserFollower follow, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserFollowers.AddAsync(follow, cancellationToken).ConfigureAwait(false);
    }

    public void Remove(UserFollower follow)
    {
        _dbContext.UserFollowers.Remove(follow);
    }
}

