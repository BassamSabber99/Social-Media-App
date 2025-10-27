using Microsoft.AspNetCore.SignalR;

namespace SocialMediaApp.Api.Realtime;

public static class HubUserHelper
{
    public static Guid? GetUserId(HubCallerContext context)
    {
        var userIdClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static string? GetUserName(HubCallerContext context)
    {
        return context.User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
    }
}