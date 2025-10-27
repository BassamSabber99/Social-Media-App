using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Comment>> GetForPostAsync(Guid postId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountForPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
    void Remove(Comment comment);
}

