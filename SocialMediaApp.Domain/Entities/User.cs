namespace SocialMediaApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<UserFollower> Followers { get; set; } = new List<UserFollower>();
    public ICollection<UserFollower> Following { get; set; } = new List<UserFollower>();
    public ICollection<Chat> ChatsAsUser1 { get; set; } = new List<Chat>();
    public ICollection<Chat> ChatsAsUser2 { get; set; } = new List<Chat>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

