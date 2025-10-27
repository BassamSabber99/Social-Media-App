using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> SearchAsync(string query, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}

