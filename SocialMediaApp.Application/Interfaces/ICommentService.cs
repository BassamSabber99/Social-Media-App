using SocialMediaApp.Application.DTOs;

namespace SocialMediaApp.Application.Interfaces;

public interface ICommentService
{
    Task<CommentDto> CreateCommentAsync(Guid postId, Guid authorId, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsForPostAsync(Guid postId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountCommentsForPostAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default);
}

