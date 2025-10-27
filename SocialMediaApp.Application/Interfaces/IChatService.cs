using SocialMediaApp.Application.DTOs;

namespace SocialMediaApp.Application.Interfaces;

public interface IChatService
{
    Task<IReadOnlyList<ChatDto>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string content, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountChatMessagesAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task MarkMessagesAsReadAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> GetOrCreateChatAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default);
}

