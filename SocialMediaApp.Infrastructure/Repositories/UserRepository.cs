using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(string query, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.ToLowerInvariant();
        
        return await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.UserName.ToLower().Contains(normalizedQuery) ||
                       u.DisplayName.ToLower().Contains(normalizedQuery) ||
                       u.Email.ToLower().Contains(normalizedQuery))
            .OrderBy(u => u.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }
}

