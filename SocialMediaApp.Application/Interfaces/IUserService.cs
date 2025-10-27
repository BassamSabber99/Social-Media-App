using SocialMediaApp.Application.DTOs;

namespace SocialMediaApp.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> SearchUsersAsync(string query, Guid requesterId, int skip, int take, CancellationToken cancellationToken = default);
    Task FollowUserAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
    Task UnfollowUserAsync(Guid followerId, Guid followedId, CancellationToken cancellationToken = default);
}

