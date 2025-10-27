using Microsoft.AspNetCore.Mvc;
using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;
using System.Security.Claims;

namespace SocialMediaApp.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chats").RequireAuthorization();

        group.MapGet("/", GetUserChats);
        group.MapGet("/{chatId:guid}/messages", GetChatMessages);
        group.MapPost("/", SendMessage);
        group.MapPost("/{chatId:guid}/read", MarkAsRead);
        group.MapPost("/create/{userId:guid}", GetOrCreateChat);

        return app;
    }

    private static async Task<IResult> GetUserChats(
        [FromServices] IChatService chatService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var chats = await chatService.GetUserChatsAsync(userId.Value, cancellationToken);
        return Results.Ok(chats);
    }

    private static async Task<IResult> GetChatMessages(
        [FromRoute] Guid chatId,
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] IChatService chatService,
        ClaimsPrincipal user,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        var messages = await chatService.GetChatMessagesAsync(chatId, userId.Value, skip, take, cancellationToken);
        var total = await chatService.CountChatMessagesAsync(chatId, cancellationToken);

        httpContext.Response.Headers["X-Total-Count"] = total.ToString();
        return Results.Ok(messages);
    }

    private static async Task<IResult> SendMessage(
        [FromBody] SendMessageRequest request,
        [FromServices] IChatService chatService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { error = "Content is required" });
        }

        var message = await chatService.SendMessageAsync(userId.Value, request.ReceiverId, request.Content, cancellationToken);
        return Results.Ok(message);
    }

    private static async Task<IResult> MarkAsRead(
        [FromRoute] Guid chatId,
        [FromServices] IChatService chatService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        await chatService.MarkMessagesAsReadAsync(chatId, userId.Value, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> GetOrCreateChat(
        [FromRoute] Guid userId,
        [FromServices] IChatService chatService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetUserId(user);
        if (!currentUserId.HasValue)
        {
            return Results.Unauthorized();
        }

        var chatId = await chatService.GetOrCreateChatAsync(currentUserId.Value, userId, cancellationToken);
        if (!chatId.HasValue)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        return Results.Ok(new { chatId });
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}

