using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, Guid requesterId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = take <= 0 ? 20 : Math.Min(50, take);

        var users = await _unitOfWork.Users.SearchAsync(query, normalizedSkip, normalizedTake, cancellationToken).ConfigureAwait(false);
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            result.Add(await MapUserToDtoAsync(user, requesterId, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    public async Task FollowUserAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        if (followerId == followedId)
        {
            throw new InvalidOperationException("Users cannot follow themselves.");
        }

        var follower = await _unitOfWork.Users.GetByIdAsync(followerId, cancellationToken).ConfigureAwait(false);
        if (follower is null)
        {
            throw new InvalidOperationException($"Follower '{followerId}' not found.");
        }

        var followed = await _unitOfWork.Users.GetByIdAsync(followedId, cancellationToken).ConfigureAwait(false);
        if (followed is null)
        {
            throw new InvalidOperationException($"User '{followedId}' not found.");
        }

        var exists = await _unitOfWork.UserFollowers.ExistsAsync(followerId, followedId, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            return; // Already following
        }

        var userFollower = new UserFollower
        {
            FollowerId = followerId,
            FollowedId = followedId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.UserFollowers.AddAsync(userFollower, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnfollowUserAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default)
    {
        var userFollower = await _unitOfWork.UserFollowers.GetAsync(followerId, followedId, cancellationToken).ConfigureAwait(false);
        if (userFollower is null)
        {
            return; // Not following
        }

        _unitOfWork.UserFollowers.Remove(userFollower);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<UserDto> MapUserToDtoAsync(User user, Guid requesterId, CancellationToken cancellationToken)
    {
        var followersCountTask = _unitOfWork.UserFollowers.CountFollowersAsync(user.Id, cancellationToken);
        var followingCountTask = _unitOfWork.UserFollowers.CountFollowingAsync(user.Id, cancellationToken);
        var isFollowedTask = requesterId == Guid.Empty || requesterId == user.Id
            ? Task.FromResult(false)
            : _unitOfWork.UserFollowers.ExistsAsync(requesterId, user.Id, cancellationToken);

        await Task.WhenAll(followersCountTask, followingCountTask, isFollowedTask).ConfigureAwait(false);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAtUtc = user.CreatedAtUtc,
            FollowersCount = followersCountTask.Result,
            FollowingCount = followingCountTask.Result,
            IsFollowedByRequester = isFollowedTask.Result
        };
    }
}

