namespace SocialMediaApp.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IPostRepository Posts { get; }
    IUserRepository Users { get; }
    ICommentRepository Comments { get; }
    ILikeRepository Likes { get; }
    IUserFollowerRepository UserFollowers { get; }
    IChatRepository Chats { get; }
    IMessageRepository Messages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

