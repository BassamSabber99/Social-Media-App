using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using SocialMediaApp.Application.Configuration;
using SocialMediaApp.Application.Interfaces;
using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Application.Services;
using SocialMediaApp.Infrastructure.Persistence;
using SocialMediaApp.Infrastructure.Repositories;
using SocialMediaApp.Infrastructure.Services;
using SocialMediaApp.Infrastructure.UnitOfWork;

namespace SocialMediaApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=socialmedia.db";
        connectionString = NormalizeSqliteConnectionString(connectionString, dataDirectory);

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<IUserFollowerRepository, UserFollowerRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();

        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IChatService, ChatService>();

        // Configure MinIO client
        var fileStorageOptions = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>() 
            ?? new FileStorageOptions();

        services.AddSingleton<IMinioClient>(sp =>
        {
            var client = new MinioClient()
                .WithEndpoint(fileStorageOptions.MinIO.Endpoint)
                .WithCredentials(fileStorageOptions.MinIO.AccessKey, fileStorageOptions.MinIO.SecretKey);

            if (fileStorageOptions.MinIO.UseSSL)
            {
                client = client.WithSSL();
            }

            return client.Build();
        });

        services.AddScoped<IFileStorageService, MinIOFileStorageService>();

        return services;
    }

    private static string NormalizeSqliteConnectionString(string connectionString, string dataDirectory)
    {
        return connectionString.Contains("App_Data", StringComparison.OrdinalIgnoreCase)
            ? connectionString.Replace("App_Data", dataDirectory, StringComparison.OrdinalIgnoreCase)
            : connectionString;
    }
}

