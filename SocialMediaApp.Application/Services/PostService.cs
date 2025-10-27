using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Services;

public sealed class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;

    public PostService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PostDto> CreatePostAsync(Guid authorId, string content, string? imageUrl, CancellationToken cancellationToken = default)
    {
        var author = await _unitOfWork.Users.GetByIdAsync(authorId, cancellationToken).ConfigureAwait(false);
        if (author is null)
        {
            throw new InvalidOperationException($"Author '{authorId}' not found.");
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Content = content,
            ImageUrl = imageUrl,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        await _unitOfWork.Posts.AddAsync(post, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = await BuildPostDtoAsync(post.Id, authorId, cancellationToken).ConfigureAwait(false);

        return dto;
    }

    public async Task<PostDto?> GetPostAsync(Guid postId, Guid requesterId, CancellationToken cancellationToken = default)
    {
        var post = await _unitOfWork.Posts.GetDetailedByIdAsync(postId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            return null;
        }

        return await MapPostAsync(post, requesterId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PostDto>> GetFeedAsync(Guid requesterId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = take <= 0 ? 20 : Math.Min(50, take);

        var posts = await _unitOfWork.Posts.GetFeedAsync(requesterId, normalizedSkip, normalizedTake, cancellationToken).ConfigureAwait(false);
        var results = new List<PostDto>(posts.Count);

        foreach (var post in posts)
        {
            results.Add(await MapPostAsync(post, requesterId, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }

    public Task<int> CountFeedAsync(Guid requesterId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.Posts.CountFeedAsync(requesterId, cancellationToken);
    }

    public async Task LikePostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Likes.GetAsync(postId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var like = new Like
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Likes.AddAsync(like, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnlikePostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Likes.GetAsync(postId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        _unitOfWork.Likes.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PostDto> BuildPostDtoAsync(Guid postId, Guid requesterId, CancellationToken cancellationToken)
    {
        var post = await _unitOfWork.Posts.GetDetailedByIdAsync(postId, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException($"Post '{postId}' not found.");

        return await MapPostAsync(post, requesterId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PostDto> MapPostAsync(Post post, Guid requesterId, CancellationToken cancellationToken)
    {
        var commentCountTask = _unitOfWork.Comments.CountForPostAsync(post.Id, cancellationToken);
        var likeCountTask = _unitOfWork.Likes.CountForPostAsync(post.Id, cancellationToken);
        var isLikedTask = requesterId == Guid.Empty
            ? Task.FromResult(false)
            : _unitOfWork.Likes.IsPostLikedByUserAsync(post.Id, requesterId, cancellationToken);
        var isAuthorFollowedTask = requesterId == Guid.Empty || requesterId == post.AuthorId
            ? Task.FromResult(false)
            : _unitOfWork.UserFollowers.ExistsAsync(requesterId, post.AuthorId, cancellationToken);

        await Task.WhenAll(commentCountTask, likeCountTask, isLikedTask, isAuthorFollowedTask).ConfigureAwait(false);

        return new PostDto
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorUserName = post.Author?.UserName ?? string.Empty,
            AuthorDisplayName = post.Author?.DisplayName ?? string.Empty,
            Content = post.Content,
            ImageUrl = post.ImageUrl,
            CreatedAtUtc = post.CreatedAtUtc,
            UpdatedAtUtc = post.UpdatedAtUtc,
            CommentCount = commentCountTask.Result,
            LikeCount = likeCountTask.Result,
            IsLikedByRequester = isLikedTask.Result,
            IsAuthorFollowedByRequester = isAuthorFollowedTask.Result
        };
    }
}

