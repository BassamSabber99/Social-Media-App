using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Message>> GetChatMessagesAsync(Guid chatId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountChatMessagesAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<int> CountUnreadMessagesAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
}

