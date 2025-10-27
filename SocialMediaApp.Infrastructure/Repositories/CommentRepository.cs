using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public class CommentRepository(AppDbContext dbContext) : ICommentRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<Comment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Comments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Comment>> GetForPostAsync(Guid postId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Comments
            .Include(c => c.Author)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> CountForPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Comments
            .AsNoTracking()
            .CountAsync(c => c.PostId == postId, cancellationToken)
            .ConfigureAwait(false);
    }

    public void Remove(Comment comment)
    {
        _dbContext.Comments.Remove(comment);
    }
}

