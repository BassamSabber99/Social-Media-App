using SocialMediaApp.Application.Interfaces.Repositories;
using SocialMediaApp.Infrastructure.Persistence;
using SocialMediaApp.Infrastructure.Repositories;

namespace SocialMediaApp.Infrastructure.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext,
                      IPostRepository posts,
                      IUserRepository users,
                      ICommentRepository comments,
                      ILikeRepository likes,
                      IUserFollowerRepository userFollowers,
                      IChatRepository chats,
                      IMessageRepository messages)
    {
        _dbContext = dbContext;
        Posts = posts;
        Users = users;
        Comments = comments;
        Likes = likes;
        UserFollowers = userFollowers;
        Chats = chats;
        Messages = messages;
    }

    public IPostRepository Posts { get; }
    public IUserRepository Users { get; }
    public ICommentRepository Comments { get; }
    public ILikeRepository Likes { get; }
    public IUserFollowerRepository UserFollowers { get; }
    public IChatRepository Chats { get; }
    public IMessageRepository Messages { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _dbContext.DisposeAsync();
    }
}

