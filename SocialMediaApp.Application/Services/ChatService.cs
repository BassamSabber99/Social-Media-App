using Microsoft.Extensions.Options;
using SocialMediaApp.Application.Configuration;
using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;
using SocialMediaApp.Domain.Enums;

namespace SocialMediaApp.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly FileStorageOptions _fileStorageOptions;

    public ChatService(
        IUnitOfWork unitOfWork, 
        IFileStorageService fileStorageService,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _fileStorageOptions = fileStorageOptions.Value;
    }

    public async Task<IReadOnlyList<ChatDto>> GetUserChatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var chats = await _unitOfWork.Chats.GetUserChatsAsync(userId, cancellationToken);
        var chatDtos = new List<ChatDto>();

        foreach (var chat in chats)
        {
            var otherUser = chat.User1Id == userId ? chat.User2 : chat.User1;
            if (otherUser == null) continue;

            var unreadCount = await _unitOfWork.Messages.CountUnreadMessagesAsync(chat.Id, userId, cancellationToken);
            var messages = await _unitOfWork.Messages.GetChatMessagesAsync(chat.Id, 0, 1, cancellationToken);
            var lastMessage = messages.FirstOrDefault();

            chatDtos.Add(new ChatDto
            {
                Id = chat.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.UserName,
                OtherUserDisplayName = otherUser.DisplayName,
                OtherUserProfileImageUrl = otherUser.ProfileImageUrl,
                LastMessageAtUtc = chat.LastMessageAtUtc,
                LastMessageContent = lastMessage?.Content,
                UnreadCount = unreadCount
            });
        }

        return chatDtos;
    }

    public async Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string content, CancellationToken cancellationToken = default)
    {
        // Prevent messaging yourself
        if (senderId == receiverId)
        {
            throw new InvalidOperationException("Cannot send messages to yourself");
        }

        // Get or create chat
        var chat = await _unitOfWork.Chats.GetByUsersAsync(senderId, receiverId, cancellationToken);
        
        if (chat == null)
        {
            chat = new Chat
            {
                Id = Guid.NewGuid(),
                User1Id = senderId,
                User2Id = receiverId,
                CreatedAtUtc = DateTime.UtcNow,
                LastMessageAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.Chats.AddAsync(chat, cancellationToken);
        }
        else
        {
            chat.LastMessageAtUtc = DateTime.UtcNow;
            _unitOfWork.Chats.Update(chat);
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            SenderId = senderId,
            Content = content,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var sender = await _unitOfWork.Users.GetByIdAsync(senderId, cancellationToken);

        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderUserName = sender?.UserName ?? string.Empty,
            SenderDisplayName = sender?.DisplayName ?? string.Empty,
            Content = message.Content,
            MessageType = message.MessageType,
            FileName = message.FileName,
            FileSize = message.FileSize,
            MimeType = message.MimeType,
            IsRead = message.IsRead,
            CreatedAtUtc = message.CreatedAtUtc
        };
    }

    public async Task<MessageDto> SendFileMessageAsync(Guid senderId, Guid receiverId, Stream fileStream, string fileName, string mimeType, long fileSize, CancellationToken cancellationToken = default)
    {
        // Prevent messaging yourself
        if (senderId == receiverId)
        {
            throw new InvalidOperationException("Cannot send messages to yourself");
        }

        // Validate file size
        var maxSize = mimeType.StartsWith("audio/") ? _fileStorageOptions.MaxVoiceNoteSizeBytes : _fileStorageOptions.MaxFileSizeBytes;
        if (fileSize > maxSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {maxSize / 1024 / 1024} MB");
        }

        // Validate file type if configured
        if (_fileStorageOptions.AllowedFileTypes.Length > 0 && !_fileStorageOptions.AllowedFileTypes.Contains(mimeType))
        {
            throw new InvalidOperationException($"File type {mimeType} is not allowed");
        }

        // Upload file to storage
        var fileUrl = await _fileStorageService.UploadAsync(fileStream, fileName, mimeType, _fileStorageOptions.MinIO.BucketName, cancellationToken);

        // Get or create chat
        var chat = await _unitOfWork.Chats.GetByUsersAsync(senderId, receiverId, cancellationToken);
        
        if (chat == null)
        {
            chat = new Chat
            {
                Id = Guid.NewGuid(),
                User1Id = senderId,
                User2Id = receiverId,
                CreatedAtUtc = DateTime.UtcNow,
                LastMessageAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.Chats.AddAsync(chat, cancellationToken);
        }
        else
        {
            chat.LastMessageAtUtc = DateTime.UtcNow;
            _unitOfWork.Chats.Update(chat);
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            SenderId = senderId,
            Content = fileUrl, // Store the file URL in content
            MessageType = MessageType.File,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Messages.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var sender = await _unitOfWork.Users.GetByIdAsync(senderId, cancellationToken);

        return new MessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            SenderId = message.SenderId,
            SenderUserName = sender?.UserName ?? string.Empty,
            SenderDisplayName = sender?.DisplayName ?? string.Empty,
            Content = message.Content,
            MessageType = message.MessageType,
            FileName = message.FileName,
            FileSize = message.FileSize,
            MimeType = message.MimeType,
            IsRead = message.IsRead,
            CreatedAtUtc = message.CreatedAtUtc
        };
    }

    public async Task<IReadOnlyList<MessageDto>> GetChatMessagesAsync(Guid chatId, Guid userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var messages = await _unitOfWork.Messages.GetChatMessagesAsync(chatId, skip, take, cancellationToken);
        
        return messages.Select(m => new MessageDto
        {
            Id = m.Id,
            ChatId = m.ChatId,
            SenderId = m.SenderId,
            SenderUserName = m.Sender?.UserName ?? string.Empty,
            SenderDisplayName = m.Sender?.DisplayName ?? string.Empty,
            Content = m.Content,
            MessageType = m.MessageType,
            FileName = m.FileName,
            FileSize = m.FileSize,
            MimeType = m.MimeType,
            IsRead = m.IsRead,
            CreatedAtUtc = m.CreatedAtUtc
        }).ToList();
    }

    public Task<int> CountChatMessagesAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.Messages.CountChatMessagesAsync(chatId, cancellationToken);
    }

    public Task MarkMessagesAsReadAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _unitOfWork.Messages.MarkAsReadAsync(chatId, userId, cancellationToken);
    }

    public async Task<Guid?> GetOrCreateChatAsync(Guid user1Id, Guid user2Id, CancellationToken cancellationToken = default)
    {
        // Prevent creating chat with yourself
        if (user1Id == user2Id)
        {
            throw new InvalidOperationException("Cannot create chat with yourself");
        }

        var chat = await _unitOfWork.Chats.GetByUsersAsync(user1Id, user2Id, cancellationToken);
        
        if (chat != null)
        {
            return chat.Id;
        }

        var user1 = await _unitOfWork.Users.GetByIdAsync(user1Id, cancellationToken);
        var user2 = await _unitOfWork.Users.GetByIdAsync(user2Id, cancellationToken);

        if (user1 == null || user2 == null)
        {
            return null;
        }

        chat = new Chat
        {
            Id = Guid.NewGuid(),
            User1Id = user1Id,
            User2Id = user2Id,
            CreatedAtUtc = DateTime.UtcNow,
            LastMessageAtUtc = DateTime.UtcNow
        };

        await _unitOfWork.Chats.AddAsync(chat, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return chat.Id;
    }
}

