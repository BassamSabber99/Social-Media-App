using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocialMediaApp.Api.Realtime;

[Authorize]
public class VideoHub : Hub
{
    public override Task OnConnectedAsync()
    {
        return base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendOffer(Guid targetUserId, string sdp)
    {
        var callerId = HubUserHelper.GetUserId(Context);
        if (callerId == null) return;
        
        // Prevent calling yourself
        if (targetUserId == callerId) return;
        
        var callerName = HubUserHelper.GetUserName(Context) ?? "Unknown";
        
        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveOffer", callerId, callerName, sdp);
    }

    public async Task SendAnswer(Guid targetUserId, string sdp)
    {
        var callerId = HubUserHelper.GetUserId(Context);
        if (callerId == null) return;

        // Prevent answering yourself
        if (targetUserId == callerId) return;

        var callerName = HubUserHelper.GetUserName(Context) ?? "Unknown";
        
        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveAnswer", callerId,callerName, sdp);
    }

    public async Task SendCandidate(Guid targetUserId, string candidate)
    {
        var senderId = HubUserHelper.GetUserId(Context);
        if (senderId == null) return;
        
        // Prevent sending candidates to yourself
        if (targetUserId == senderId) return;
        
        await Clients.User(targetUserId.ToString()).SendAsync("ReceiveCandidate", senderId, candidate);
    }

    public async Task HangupCall(Guid targetUserId)
    {
        var callerId = HubUserHelper.GetUserId(Context);
        if (callerId == null) return;
        
        // Prevent sending to yourself
        if (targetUserId == callerId) return;
        
        await Clients.User(targetUserId.ToString()).SendAsync("CallHangup", callerId);
    }
}