using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Infrastructure.Persistence;

namespace SocialMediaApp.Infrastructure.Repositories;

public sealed class ChatRepository : IChatRepository
{
    private readonly AppDbContext _context;

    public ChatRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Chat?> GetByIdAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _context.Chats
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.Id == chatId, cancellationToken);
    }

    public Task<Chat?> GetByUsersAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
    {
        return _context.Chats
            .Include(c => c.User1)
            .Include(c => c.User2)
            .FirstOrDefaultAsync(c => 
                (c.User1Id == user1Id && c.User2Id == user2Id) || 
                (c.User1Id == user2Id && c.User2Id == user1Id), 
                cancellationToken);
    }

    public async Task<IReadOnlyList<Chat>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var chats = await _context.Chats
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToListAsync(cancellationToken);
        
        return chats;
    }

    public async Task AddAsync(Chat chat, CancellationToken cancellationToken = default)
    {
        await _context.Chats.AddAsync(chat, cancellationToken);
    }

    public void Update(Chat chat)
    {
        _context.Chats.Update(chat);
    }
}

