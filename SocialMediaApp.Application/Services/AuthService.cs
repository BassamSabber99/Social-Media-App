using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialMediaApp.Application.Configuration;
using SocialMediaApp.Application.DTOs.Auth;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;

    public AuthService(IUserRepository userRepository, IUnitOfWork unitOfWork, IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new AuthResponse(
            token,
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName
        );
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return null;
        }

        var existingUserName = await _userRepository.GetByUserNameAsync(request.UserName, cancellationToken);
        if (existingUserName is not null)
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            DisplayName = request.DisplayName,
            CreatedAtUtc = DateTime.UtcNow,
            LastActiveAtUtc = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);

        return new AuthResponse(
            token,
            user.Id,
            user.UserName,
            user.Email,
            user.DisplayName
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}

