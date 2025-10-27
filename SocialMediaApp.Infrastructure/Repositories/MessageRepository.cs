using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public sealed class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetChatMessagesAsync(Guid chatId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        
        return messages;
    }

    public Task<int> CountChatMessagesAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _context.Messages.CountAsync(m => m.ChatId == chatId, cancellationToken);
    }

    public Task<int> CountUnreadMessagesAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Messages
            .CountAsync(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead, cancellationToken);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        await _context.Messages
            .Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsRead, true), cancellationToken);
    }
}

