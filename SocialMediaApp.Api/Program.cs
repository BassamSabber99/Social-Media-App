using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SocialMediaApp.Api.Endpoints;
using SocialMediaApp.Api.Realtime;
using SocialMediaApp.Application.Configuration;
using SocialMediaApp.Infrastructure;
using SocialMediaApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SignalR with optional Redis backplane for horizontal scaling
var signalRBuilder = builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);
var redisConnection = builder.Configuration.GetSection("Redis")?["ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "SocialMediaApp";
    });
    Console.WriteLine($"SignalR configured with Redis backplane: {redisConnection}");
}
else
{
    Console.WriteLine("SignalR running in single-server mode (no Redis backplane)");
}

builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure JWT Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // For SignalR authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("client",
        policy => policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials());
});


/*builder.WebHost.ConfigureKestrel(options =>{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(5001, listenOptions =>
    {
        var certPfxPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "social-media-app-client", "ssl", "dev.pfx");
        var certPassword = "123456789";
        listenOptions.UseHttps(certPfxPath, certPassword);
    });
});*/

var app = builder.Build();

// Initialize database
await DbInitializer.InitializeDatabaseAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors("client");

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<VideoHub>("/hubs/video");
app.MapSocialMediaEndpoints();

app.Run();

