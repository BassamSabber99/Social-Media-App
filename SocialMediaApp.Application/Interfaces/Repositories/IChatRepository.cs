using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IChatRepository
{
    Task<Chat?> GetByIdAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Chat?> GetByUsersAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chat>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Chat chat, CancellationToken cancellationToken = default);
    void Update(Chat chat);
}

