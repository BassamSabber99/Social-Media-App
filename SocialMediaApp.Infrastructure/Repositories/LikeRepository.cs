using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public class LikeRepository(AppDbContext dbContext) : ILikeRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task AddAsync(Like like, CancellationToken cancellationToken = default)
    {
        await _dbContext.Likes.AddAsync(like, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountForPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Likes
            .AsNoTracking()
            .CountAsync(l => l.PostId == postId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Like?> GetAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> IsPostLikedByUserAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Likes
            .AsNoTracking()
            .AnyAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Remove(Like like)
    {
        _dbContext.Likes.Remove(like);
    }
}

