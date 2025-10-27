using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Api.Endpoints;

public static class CommentEndpoints
{
    public static RouteGroupBuilder MapCommentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/posts/{postId:guid}/comments").RequireAuthorization();

        group.MapGet("", GetCommentsAsync);
        group.MapPost("", CreateCommentAsync);
        group.MapDelete("/{commentId:guid}", DeleteCommentAsync);

        return group;
    }

    private static async Task<Results<Ok<IReadOnlyList<CommentDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetCommentsAsync(
        HttpContext httpContext,
        ICommentService commentService,
        Guid postId,
        [Microsoft.AspNetCore.Mvc.FromQuery] int skip = 0,
        [Microsoft.AspNetCore.Mvc.FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        if (skip < 0 || take <= 0)
        {
            return TypedResults.BadRequest("Invalid pagination parameters");
        }

        var comments = await commentService.GetCommentsForPostAsync(postId, skip, take, cancellationToken).ConfigureAwait(false);
        var totalCount = await commentService.CountCommentsForPostAsync(postId, cancellationToken).ConfigureAwait(false);

        httpContext.Response.Headers["X-Total-Count"] = totalCount.ToString();

        return TypedResults.Ok(comments);
    }

    private static async Task<Results<Created<CommentDto>, BadRequest<string>, UnauthorizedHttpResult>> CreateCommentAsync(
        HttpContext httpContext,
        ICommentService commentService,
        Guid postId,
        [Microsoft.AspNetCore.Mvc.FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return TypedResults.BadRequest("Content is required");
        }

        var comment = await commentService.CreateCommentAsync(postId, userId, request.Content, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/posts/{postId}/comments/{comment.Id}", comment);
    }

    private static async Task<Results<Ok, NotFound, UnauthorizedHttpResult>> DeleteCommentAsync(
        HttpContext httpContext,
        ICommentService commentService,
        Guid postId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        var success = await commentService.DeleteCommentAsync(commentId, userId, cancellationToken).ConfigureAwait(false);
        return success ? TypedResults.Ok() : TypedResults.NotFound();
    }

    private static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

