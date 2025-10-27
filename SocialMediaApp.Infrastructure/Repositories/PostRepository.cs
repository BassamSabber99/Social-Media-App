using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public class PostRepository(AppDbContext dbContext) : IPostRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _dbContext.Posts.AddAsync(post, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Post?> GetDetailedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .Include(p => p.Author)
            .Include(p => p.Comments)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Post>> GetFeedAsync(Guid requesterId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<int> CountFeedAsync(Guid requesterId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Posts.CountAsync(cancellationToken);
    }

    public void Remove(Post post)
    {
        _dbContext.Posts.Remove(post);
    }

    public void Update(Post post)
    {
        _dbContext.Posts.Update(post);
    }
}

