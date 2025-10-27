using Microsoft.EntityFrameworkCore;
using SocialMediaApp.Domain.Entities;

namespace SocialMediaApp.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<UserFollower> UserFollowers => Set<UserFollower>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.UserName).IsUnique();
            entity.Property(u => u.UserName).HasMaxLength(32).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(64);
            entity.Property(u => u.Bio).HasMaxLength(512);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Content).HasMaxLength(1024);
            entity.HasOne(p => p.Author)
                  .WithMany(u => u.Posts)
                  .HasForeignKey(p => p.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Content).HasMaxLength(512);
            entity.HasOne(c => c.Post)
                  .WithMany(p => p.Comments)
                  .HasForeignKey(c => c.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.Author)
                  .WithMany(u => u.Comments)
                  .HasForeignKey(c => c.AuthorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => new { l.PostId, l.UserId }).IsUnique();
            entity.HasOne(l => l.Post)
                  .WithMany(p => p.Likes)
                  .HasForeignKey(l => l.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(l => l.User)
                  .WithMany(u => u.Likes)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserFollower>(entity =>
        {
            entity.HasKey(f => new { f.FollowerId, f.FollowedId });
            entity.HasOne(f => f.Follower)
                  .WithMany(u => u.Following)
                  .HasForeignKey(f => f.FollowerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.Followed)
                  .WithMany(u => u.Followers)
                  .HasForeignKey(f => f.FollowedId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.User1Id, c.User2Id }).IsUnique();
            entity.HasOne(c => c.User1)
                  .WithMany(u => u.ChatsAsUser1)
                  .HasForeignKey(c => c.User1Id)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(c => c.User2)
                  .WithMany(u => u.ChatsAsUser2)
                  .HasForeignKey(c => c.User2Id)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Content).HasMaxLength(2048);
            entity.HasOne(m => m.Chat)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(m => m.ChatId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.Sender)
                  .WithMany(u => u.Messages)
                  .HasForeignKey(m => m.SenderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

