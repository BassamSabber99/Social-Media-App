using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SocialMediaApp.Application.DTOs;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/search", SearchUsersAsync);
        group.MapPost("/{userId:guid}/follow", FollowUserAsync);
        group.MapDelete("/{userId:guid}/follow", UnfollowUserAsync);

        return group;
    }

    private static async Task<Results<Ok<IReadOnlyList<UserDto>>, BadRequest<string>, UnauthorizedHttpResult>> SearchUsersAsync(
        HttpContext httpContext,
        IUserService userService,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? query = null,
        [Microsoft.AspNetCore.Mvc.FromQuery] int skip = 0,
        [Microsoft.AspNetCore.Mvc.FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdFromClaims(httpContext.User);
        if (userId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return TypedResults.BadRequest("Query parameter is required");
        }

        if (skip < 0 || take <= 0)
        {
            return TypedResults.BadRequest("Invalid pagination parameters");
        }

        var users = await userService.SearchUsersAsync(query, userId, skip, take, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(users);
    }

    private static async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> FollowUserAsync(
        HttpContext httpContext,
        IUserService userService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var followerId = GetUserIdFromClaims(httpContext.User);
        if (followerId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        if (followerId == userId)
        {
            return TypedResults.BadRequest("Users cannot follow themselves");
        }

        try
        {
            await userService.FollowUserAsync(followerId, userId, cancellationToken).ConfigureAwait(false);
            return TypedResults.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok, UnauthorizedHttpResult>> UnfollowUserAsync(
        HttpContext httpContext,
        IUserService userService,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var followerId = GetUserIdFromClaims(httpContext.User);
        if (followerId == Guid.Empty)
        {
            return TypedResults.Unauthorized();
        }

        await userService.UnfollowUserAsync(followerId, userId, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok();
    }

    private static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

