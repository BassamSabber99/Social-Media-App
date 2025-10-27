using Microsoft.AspNetCore.Mvc;
using SocialMediaApp.Application.DTOs.Auth;
using SocialMediaApp.Application.Interfaces;

namespace SocialMediaApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithOpenApi();
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || 
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new { error = "All fields are required" });
        }

        var response = await authService.RegisterAsync(request, cancellationToken);

        if (response is null)
        {
            return Results.BadRequest(new { error = "User with this email or username already exists" });
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required" });
        }

        var response = await authService.LoginAsync(request, cancellationToken);

        if (response is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(response);
    }
}

