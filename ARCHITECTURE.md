# Architecture Documentation

## Overview

This social media application follows Clean Architecture principles with clear separation of concerns across multiple layers. The backend uses .NET 9 with a layered architecture, while the frontend is built with Angular 19 using standalone components and signals.

## Backend Architecture

### Layer Structure

```
┌─────────────────────────────────────────────────────┐
│                 Presentation Layer                   │
│              (SocialMediaApp.Api)                    │
│  ┌──────────────────┐    ┌────────────────────┐    │
│  │  HTTP Endpoints  │    │  SignalR Hubs      │    │
│  │  - Auth          │    │  - ChatHub         │    │
│  │  - Posts         │    │  - VideoHub        │    │
│  │  - Comments      │    └────────────────────┘    │
│  │  - Users         │                               │
│  │  - Chats         │                               │
│  └──────────────────┘                               │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│              Application Layer                       │
│         (SocialMediaApp.Application)                 │
│  ┌──────────────────┐    ┌────────────────────┐    │
│  │  Services        │    │  Interfaces        │    │
│  │  - AuthService   │    │  - IAuthService    │    │
│  │  - PostService   │    │  - IPostService    │    │
│  │  - ChatService   │    │  - IChatService    │    │
│  └──────────────────┘    └────────────────────┘    │
│  ┌──────────────────┐    ┌────────────────────┐    │
│  │  DTOs            │    │  Configuration     │    │
│  └──────────────────┘    └────────────────────┘    │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│                 Domain Layer                         │
│            (SocialMediaApp.Domain)                   │
│  ┌──────────────────────────────────────────────┐  │
│  │  Entities                                     │  │
│  │  - User, Post, Comment, Like                 │  │
│  │  - UserFollower, Chat, Message               │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│            Infrastructure Layer                      │
│        (SocialMediaApp.Infrastructure)               │
│  ┌──────────────────┐    ┌────────────────────┐    │
│  │  Repositories    │    │  DbContext         │    │
│  │  - UserRepo      │    │  - AppDbContext    │    │
│  │  - PostRepo      │    └────────────────────┘    │
│  │  - ChatRepo      │    ┌────────────────────┐    │
│  └──────────────────┘    │  Services          │    │
│                           │  - PasswordHasher  │    │
│                           └────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

### Design Patterns

#### 1. Repository Pattern
Abstracts data access logic and provides a collection-like interface for accessing domain objects.

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> SearchAsync(string query, int skip, int take, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}
```

#### 2. Unit of Work Pattern
Maintains a list of objects affected by a business transaction and coordinates the writing of changes.

```csharp
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IChatRepository Chats { get; }
    IMessageRepository Messages { get; }
    ILikeRepository Likes { get; }
    IUserFollowerRepository UserFollowers { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

#### 3. Dependency Injection
All dependencies are injected through constructors, promoting loose coupling and testability.

```csharp
public sealed class PostService : IPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeedNotifier _feedNotifier;

    public PostService(IUnitOfWork unitOfWork, IFeedNotifier feedNotifier)
    {
        _unitOfWork = unitOfWork;
        _feedNotifier = feedNotifier;
    }
}
```

#### 4. DTO (Data Transfer Object) Pattern
Separates domain models from API contracts.

```csharp
public sealed record PostDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public UserDto Author { get; init; } = null!;
    public int LikesCount { get; init; }
    public int CommentsCount { get; init; }
    public bool IsLikedByRequester { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

## Frontend Architecture

### Component Structure

```
App Component
├── Auth Module
│   ├── Login Component
│   └── Register Component
├── Feed Module
│   └── Feed Page Component
│       ├── Post List
│       ├── Post Creation Form
│       └── Comment Section
├── Users Module
│   └── User Discovery Component
│       ├── Search Bar
│       └── User Cards
├── Chat Module
│   └── Chat Page Component
│       ├── Chat List Sidebar
│       ├── Chat Conversation
│       └── Video Call Button
├── Video Call Module
│   ├── Video Call Component
│   │   ├── Remote Video View
│   │   ├── Local Video (PIP)
│   │   └── Call Controls
│   └── Incoming Call Modal
└── Shared
    └── Navbar Component
```

### Service Layer

#### Core Services

**AuthService**
- Manages authentication state
- Stores JWT tokens
- Provides login/logout functionality
- Exposes observables for auth state

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  
  login(credentials: LoginRequest): Observable<AuthResponse>
  register(data: RegisterRequest): Observable<AuthResponse>
  logout(): void
  getToken(): string | null
  getUserId(): string | null
}
```

**FeedService**
- Manages post operations
- Handles feed pagination
- Provides feed update notifications

```typescript
@Injectable({ providedIn: 'root' })
export class FeedService {
  readonly feedUpdates$ = new Subject<void>();
  
  getFeed(skip: number, take: number): Observable<FeedResponseDto>
  createPost(payload: CreatePostRequest): Observable<PostDto>
  likePost(postId: string): Observable<void>
  unlikePost(postId: string): Observable<void>
  getComments(postId: string): Observable<CommentsResponseDto>
  createComment(postId: string, content: string): Observable<CommentDto>
}
```

**ChatService**
- Manages SignalR connection with automatic reconnection
- Handles real-time messaging with HTTP fallback
- Connection status monitoring and notifications
- Exponential backoff reconnection strategy
- Provides message, read receipt, and connection status observables

```typescript
@Injectable({ providedIn: 'root' })
export class ChatService {
  readonly messages$ = new Subject<MessageDto>();
  readonly messagesRead$ = new Subject<string>();
  readonly connectionStatus$ = new Subject<boolean>();
  
  // HTTP API methods
  getUserChats(): Observable<ChatDto[]>
  getChatMessages(chatId: string): Observable<MessageDto[]>
  sendMessage(request: SendMessageRequest): Observable<MessageDto>
  markAsRead(chatId: string): Observable<void>
  
  // SignalR methods
  isConnected(): boolean
  getConnectionStatus(): string
  sendMessageViaHub(receiverId: string, content: string): Promise<void>
  sendMessageWithFallback(receiverId: string, content: string): Promise<MessageDto | void>
  markAsReadViaHub(chatId: string): void
}
```

### State Management

#### Angular Signals
The application uses Angular signals for reactive state management:

```typescript
// Component state
chats = signal<ChatDto[]>([]);
selectedChat = signal<ChatDto | null>(null);
messages = signal<MessageDto[]>([]);

// Computed values
sortedChats = computed(() => {
  const chatList = [...this.chats()];
  return chatList.sort((a, b) => 
    new Date(b.lastMessageAtUtc).getTime() - 
    new Date(a.lastMessageAtUtc).getTime()
  );
});
```

### Routing Architecture

```typescript
const routes: Routes = [
  { path: '', redirectTo: '/feed', pathMatch: 'full' },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes')
  },
  {
    path: 'feed',
    loadChildren: () => import('./features/feed/feed.routes'),
    canActivate: [authGuard]
  },
  {
    path: 'users',
    loadChildren: () => import('./features/users/users.routes'),
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadChildren: () => import('./features/chat/chat.routes'),
    canActivate: [authGuard]
  }
];
```

## Real-time Communication Architecture

### SignalR Integration

```
Client                          Server
  │                               │
  ├──────── Connect ──────────────>│
  │                               │
  │<──── OnConnectedAsync ────────┤
  │      AddToGroup(user-{id})    │
  │                               │
  ├──── SendMessage ──────────────>│
  │      (receiverId, content)    │
  │                               │
  │                         [Process Message]
  │                         [Save to Database]
  │                               │
  │<──── ReceiveMessage ──────────┤
  │      (messageDto)             │
  │                               │
  [Notify Receiver]               │
  │                               │
  │<──── ReceiveMessage ──────────┤
  │      (messageDto)             │
  │                               │
```

### Connection Management

**Server-Side Hub**
```csharp
[Authorize]
public class ChatHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(Guid receiverId, string content)
    {
        var message = await _chatService.SendMessageAsync(senderId, receiverId, content);
        
        await Clients.Group($"user-{senderId}").SendAsync("ReceiveMessage", message);
        await Clients.Group($"user-{receiverId}").SendAsync("ReceiveMessage", message);
    }
}
```

**Client-Side Connection**
```typescript
private initSignalR(): void {
  const token = this.authService.getToken();
  const hubUrl = `${this.baseUrl}/hubs/chat?access_token=${token}`;
  
  this.hubConnection = new HubConnectionBuilder()
    .withUrl(hubUrl)
    .withAutomaticReconnect()
    .build();

  this.hubConnection.on('ReceiveMessage', (message: MessageDto) => {
    this.messageSubject.next(message);
  });

  this.hubConnection.start();
}
```

## Data Flow

### Creating a Post

```
User Action (Frontend)
    │
    ▼
FeedService.createPost()
    │
    ▼
HTTP POST /api/posts
    │
    ▼
PostEndpoints.CreatePostAsync()
    │
    ▼
PostService.CreatePostAsync()
    │
    ├─> Validate input
    ├─> Create Post entity
    ├─> Save to database
    └─> Notify feed update
    │
    ▼
Return PostDto
    │
    ▼
Update UI with new post
```

### Sending a Message (Hybrid Approach)

```
User Types Message
    │
    ▼
ChatService.sendMessageWithFallback()
    │
    ├─> Try: SignalR sendMessageViaHub()
    │   │
    │   ├─> ✅ Success: SignalR Connected
    │   │   │
    │   │   ▼
    │   │   SignalR: invoke('SendMessage')
    │   │   │
    │   │   ▼
    │   │   ChatHub.SendMessage()
    │   │   │
    │   │   ├─> Validate user
    │   │   ├─> Save message to database
    │   │   └─> Broadcast to both users
    │   │   │
    │   │   ▼
    │   │   SignalR: on('ReceiveMessage')
    │   │   │
    │   │   ├─> Update sender's UI (instant)
    │   │   └─> Update receiver's UI (instant)
    │   │
    │   └─> ❌ Failed: SignalR Disconnected
    │       │
    │       ▼
    │       Fallback to HTTP API
    │
    └─> Fallback: POST /api/chats
        │
        ├─> Save message to database
        ├─> Return saved message
        │
        ▼
        Manually update sender's UI
        (Receiver needs to refresh/poll)
```

## Security Architecture

### Authentication Flow

```
1. User Login Request
   └─> AuthService.LoginAsync()
       ├─> Validate credentials
       ├─> Hash and compare password
       └─> Generate JWT token

2. JWT Token Contains:
   ├─> User ID (NameIdentifier claim)
   ├─> Username
   ├─> Email
   └─> Expiration time

3. API Request with Token
   └─> JWT Bearer Authentication
       ├─> Validate token signature
       ├─> Check expiration
       └─> Extract user claims

4. Authorization
   └─> All protected endpoints require [Authorize]
```

### Password Security

```csharp
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
```

## Database Design

### Entity Relationships

```
User ─┬─< Posts (1:N)
      ├─< Comments (1:N)
      ├─< Likes (1:N)
      ├─< Messages (1:N)
      ├─< UserFollowers (Follower) (1:N)
      └─< UserFollowers (Followed) (1:N)

Post ─┬─< Comments (1:N)
      └─< Likes (1:N)

Chat ─┬─< Messages (1:N)
      ├─> User1 (N:1)
      └─> User2 (N:1)
```

### Indexing Strategy

```csharp
// Optimized queries with indexes
modelBuilder.Entity<Post>()
    .HasIndex(p => p.CreatedAtUtc);

modelBuilder.Entity<UserFollower>()
    .HasIndex(uf => new { uf.FollowerId, uf.FollowedId })
    .IsUnique();

modelBuilder.Entity<Chat>()
    .HasIndex(c => new { c.User1Id, c.User2Id })
    .IsUnique();

modelBuilder.Entity<Message>()
    .HasIndex(m => new { m.ChatId, m.CreatedAtUtc });
```

## Performance Considerations

### Backend Optimizations

1. **Async/Await Pattern**: All database operations are asynchronous
2. **Pagination**: Large datasets are paginated to reduce memory usage
3. **Eager/Lazy Loading**: Strategic use of Include() for related data
4. **Query Optimization**: Efficient LINQ queries with proper filtering
5. **Connection Pooling**: EF Core manages database connections efficiently

### Frontend Optimizations

1. **Lazy Loading**: Routes and modules load on demand
2. **OnPush Change Detection**: Optimized component updates
3. **Signals**: Efficient reactive state management
4. **Debounced Search**: Reduces API calls during user input
5. **Optimistic Updates**: Immediate UI feedback before server confirmation

## Scalability Considerations

### Horizontal Scaling

- **Stateless API**: Can run multiple instances behind load balancer
- **SignalR Backplane**: Use Redis or Azure SignalR for multi-server deployments
- **Database**: Can migrate to clustered SQL Server or PostgreSQL

### Vertical Scaling

- **Database Indexing**: Optimized for common queries
- **Caching**: Can add Redis for frequently accessed data
- **CDN**: Static assets can be served from CDN

## Testing Strategy

### Backend Testing
- **Unit Tests**: Test services in isolation with mocked repositories
- **Integration Tests**: Test endpoints with in-memory database
- **Repository Tests**: Validate data access logic

### Frontend Testing
- **Unit Tests**: Test services and components
- **Component Tests**: Test UI interactions
- **E2E Tests**: Test complete user workflows

## Deployment Architecture

```
┌─────────────────────────────────────────────┐
│         Production Environment               │
│                                              │
│  ┌────────────┐       ┌─────────────┐      │
│  │   CDN      │       │   Web App   │      │
│  │  (Static)  │       │  (Angular)  │      │
│  └────────────┘       └─────────────┘      │
│                              │               │
│                              ▼               │
│  ┌──────────────────────────────────┐      │
│  │      API Gateway / Load Balancer  │      │
│  └──────────────────────────────────┘      │
│           │              │                   │
│           ▼              ▼                   │
│  ┌──────────┐    ┌──────────┐              │
│  │  API 1   │    │  API 2   │              │
│  └──────────┘    └──────────┘              │
│           │              │                   │
│           └──────┬───────┘                  │
│                  ▼                           │
│  ┌──────────────────────────────────┐      │
│  │         Database Cluster          │      │
│  │  (SQL Server / PostgreSQL)        │      │
│  └──────────────────────────────────┘      │
│                                              │
│  ┌──────────────────────────────────┐      │
│  │       Redis / SignalR Backplane  │      │
│  └──────────────────────────────────┘      │
└─────────────────────────────────────────────┘
```

## Conclusion

This architecture provides a solid foundation for a scalable, maintainable social media application. The clean separation of concerns, combined with modern frameworks and patterns, ensures the codebase remains flexible and easy to extend with new features.

