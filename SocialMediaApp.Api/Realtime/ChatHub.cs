using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Api.Realtime;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = HubUserHelper.GetUserId(Context);
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId.Value}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = HubUserHelper.GetUserId(Context);
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId.Value}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(Guid receiverId, string content)
    {
        var senderId = HubUserHelper.GetUserId(Context);
        if (!senderId.HasValue)
        {
            throw new HubException("Unauthorized");
        }

        // Prevent messaging yourself
        if (senderId.Value == receiverId)
        {
            throw new HubException("Cannot send messages to yourself");
        }

        var message = await _chatService.SendMessageAsync(senderId.Value, receiverId, content);

        // Send to sender
        await Clients.Group($"user-{senderId.Value}").SendAsync("ReceiveMessage", message);
        
        // Send to receiver
        await Clients.Group($"user-{receiverId}").SendAsync("ReceiveMessage", message);
    }

    public async Task MarkAsRead(Guid chatId)
    {
        var userId = HubUserHelper.GetUserId(Context);
        if (!userId.HasValue)
        {
            throw new HubException("Unauthorized");
        }

        await _chatService.MarkMessagesAsReadAsync(chatId, userId.Value);
        
        await Clients.Group($"user-{userId.Value}").SendAsync("MessagesMarkedAsRead", chatId);
    }

}

