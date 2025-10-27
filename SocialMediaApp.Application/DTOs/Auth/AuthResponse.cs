namespace SocialMediaApp.Application.DTOs.Auth;

public record AuthResponse(
    string Token,
    Guid UserId,
    string UserName,
    string Email,
    string DisplayName
);

