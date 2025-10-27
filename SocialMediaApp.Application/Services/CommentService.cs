using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Services;

public sealed class CommentService : ICommentService
{
    private readonly IUnitOfWork _unitOfWork;

    public CommentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CommentDto> CreateCommentAsync(Guid postId, Guid authorId, string content, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetByIdAsync(postId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new InvalidOperationException($"Post '{postId}' not found.");
        }

        var author = await _unitOfWork.Users.GetByIdAsync(authorId, cancellationToken).ConfigureAwait(false);
        if (author is null)
        {
            throw new InvalidOperationException($"Author '{authorId}' not found.");
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            AuthorId = authorId,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            AuthorUserName = author.UserName,
            AuthorDisplayName = author.DisplayName,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc
        };
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsForPostAsync(Guid postId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = take <= 0 ? 20 : Math.Min(100, take);

        var comments = await _unitOfWork.Comments.GetForPostAsync(postId, normalizedSkip, normalizedTake, cancellationToken).ConfigureAwait(false);
        var results = new List<CommentDto>(comments.Count);

        foreach (var comment in comments)
        {
            var author = comment.Author ?? await _unitOfWork.Users.GetByIdAsync(comment.AuthorId, cancellationToken).ConfigureAwait(false);
            if (author is null) continue;

            results.Add(new CommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                AuthorUserName = author.UserName,
                AuthorDisplayName = author.DisplayName,
                Content = comment.Content,
                CreatedAtUtc = comment.CreatedAtUtc,
                UpdatedAtUtc = comment.UpdatedAtUtc
            });
        }

        return results;
    }

    public Task<int> CountCommentsForPostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.Comments.CountForPostAsync(postId, cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.GetByIdAsync(commentId, cancellationToken).ConfigureAwait(false);
        if (comment is null || comment.AuthorId != userId)
        {
            return false;
        }

        _unitOfWork.Comments.Remove(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}

