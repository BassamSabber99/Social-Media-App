using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Api.Endpoints;

public static class PostEndpoints
{
    public static RouteGroupBuilder MapPostEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/posts").RequireAuthorization();

        group.MapGet("", GetFeedAsync);
        group.MapPost("", CreatePostAsync);
        group.MapPost("/{postId:guid}/like", LikePostAsync);
        group.MapDelete("/{postId:guid}/like", UnlikePostAsync);

        return group;
    }

    private static async Task<Results<Ok<IReadOnlyList<PostDto>>, BadRequest<string>, UnauthorizedHttpResult>> GetFeedAsync(
        HttpContext httpContext,
        IPostService postService,
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

        var posts = await postService.GetFeedAsync(userId, skip, take, cancellationToken).ConfigureAwait(false);
        var totalCount = await postService.CountFeedAsync(userId, cancellationToken).ConfigureAwait(false);

        httpContext.Response.Headers["X-Total-Count"] = totalCount.ToString();

        return TypedResults.Ok(posts);
    }

    private static async Task<Results<Created<PostDto>, BadRequest<string>, UnauthorizedHttpResult>> CreatePostAsync(
        HttpContext httpContext,
        IPostService postService,
        [Microsoft.AspNetCore.Mvc.FromBody] CreatePostRequest request,
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

        var post = await postService.CreatePostAsync(userId, request.Content, request.ImageUrl, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/posts/{post.Id}", post);
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> LikePostAsync(
        HttpContext httpContext,
        IPostService postService,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        await postService.LikePostAsync(postId, userId, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> UnlikePostAsync(
        HttpContext httpContext,
        IPostService postService,
        Guid postId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        await postService.UnlikePostAsync(postId, userId, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok();
    }

    private static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
        {
            return Guid.Empty;
        }
        return userId;
    }
}

public sealed record CreatePostRequest(string Content, string? ImageUrl);

