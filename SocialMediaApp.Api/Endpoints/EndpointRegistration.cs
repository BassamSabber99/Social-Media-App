namespace SocialMediaApp.Api.Endpoints;

public static class EndpointRegistration
{
    public static void MapSocialMediaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapPostEndpoints();
        app.MapCommentEndpoints();
        app.MapUserEndpoints();
        app.MapChatEndpoints();
    }
}

