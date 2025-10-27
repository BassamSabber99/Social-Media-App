# Social Media Application

A full-stack real-time social media platform built with .NET 9 and Angular 19, featuring posts, comments, likes, user following, and real-time messaging.

## 🚀 Features

### Core Social Features
- **User Authentication & Authorization**
  - JWT-based authentication
  - Secure user registration and login
  - Token-based API protection

- **Social Feed**
  - Create and view posts with optional images
  - Like/unlike posts with real-time counter updates
  - Comment on posts with threaded discussions
  - Delete own comments
  - Infinite scroll pagination

- **User Discovery & Following**
  - Search users by name or username
  - Follow/unfollow users
  - View follower and following counts
  - Personalized feed based on followed users

- **Real-time Messaging**
  - One-on-one chat conversations
  - Real-time message delivery via SignalR WebSockets
  - **Hybrid messaging system** with automatic HTTP fallback
  - Connection status indicator (Real-time/Offline mode)
  - Automatic reconnection with exponential backoff
  - Message read receipts
  - Unread message counters
  - Chat list sorted by most recent activity
  - Direct messaging from user profiles
  - Guaranteed message delivery even during network issues

- **Video Calling**
  - One-on-one video calls with WebRTC
  - Real-time signaling via SignalR
  - Audio/video toggle controls
  - Incoming call notifications with accept/decline
  - Echo cancellation and noise suppression
  - Optimized bandwidth allocation for quality
  - Responsive video interface for mobile
  - Direct peer-to-peer media streams

### Technical Features
- **Real-time Updates**: SignalR WebSocket connections with HTTP fallback for reliability
- **Automatic Reconnection**: Exponential backoff strategy for connection resilience
- **Connection Status Monitoring**: Visual indicators for connection state
- **Responsive Design**: Mobile-first UI with modern animations
- **Clean Architecture**: Separation of concerns with layered architecture
- **RESTful API**: Well-structured HTTP endpoints
- **Entity Framework Core**: Code-first database with migrations
- **Type Safety**: Full TypeScript implementation on frontend
- **Error Handling**: Comprehensive error handling with automatic fallback mechanisms

## 🏗️ Architecture

### Backend (.NET 9)
```
SocialMediaApp/
├── SocialMediaApp.Api/              # Presentation Layer
│   ├── Endpoints/                   # Minimal API endpoints
│   │   ├── AuthEndpoints.cs
│   │   ├── PostEndpoints.cs
│   │   ├── CommentEndpoints.cs
│   │   ├── UserEndpoints.cs
│   │   └── ChatEndpoints.cs
│   └── Realtime/
│       ├── ChatHub.cs              # SignalR hub for real-time chat
│       └── VideoHub.cs             # SignalR hub for video call signaling
│
├── SocialMediaApp.Application/      # Application Layer
│   ├── DTOs/                       # Data Transfer Objects
│   ├── Interfaces/                 # Service interfaces
│   └── Services/                   # Business logic
│       ├── AuthService.cs
│       ├── PostService.cs
│       ├── CommentService.cs
│       ├── UserService.cs
│       └── ChatService.cs
│
├── SocialMediaApp.Domain/          # Domain Layer
│   └── Entities/                   # Domain entities
│       ├── User.cs
│       ├── Post.cs
│       ├── Comment.cs
│       ├── Like.cs
│       ├── UserFollower.cs
│       ├── Chat.cs
│       └── Message.cs
│
└── SocialMediaApp.Infrastructure/  # Infrastructure Layer
    ├── Persistence/
    │   └── AppDbContext.cs        # EF Core DbContext
    ├── Repositories/              # Data access implementations
    └── Migrations/                # Database migrations
```

### Frontend (Angular 19)
```
social-media-app-client/
└── src/app/
    ├── features/
    │   ├── auth/                  # Authentication components
    │   │   ├── login/
    │   │   └── register/
    │   ├── feed/                  # Social feed
    │   │   └── feed-page/
    │   ├── users/                 # User discovery
    │   │   └── user-discovery/
    │   ├── chat/                  # Real-time messaging
    │   │   └── chat-page/
    │   ├── video-call/            # Video calling
    │   │   ├── video-call.component
    │   │   └── incoming-call-modal/
    │   └── services/
    │       ├── feed.service.ts
    │       ├── chat.service.ts
    │       └── webrtc.service.ts  # WebRTC video calling
    ├── services/
    │   ├── auth.service.ts
    │   ├── user.service.ts
    │   └── signalr.service.ts     # Shared SignalR connection manager
    ├── guards/
    │   └── auth.guard.ts          # Route protection
    └── shared/
        └── navbar/                # Navigation component
```

## 🛠️ Tech Stack

### Backend
- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core** - Web framework
- **Entity Framework Core** - ORM
- **SQLite** - Database (easily switchable to SQL Server/PostgreSQL)
- **SignalR** - Real-time WebSocket communication with optional Redis backplane
- **JWT Authentication** - Secure token-based auth
- **Minimal APIs** - Modern endpoint routing

### Frontend
- **Angular 19** - Frontend framework with signals
- **TypeScript** - Type-safe JavaScript
- **RxJS** - Reactive programming
- **PrimeNG** - UI component library
- **Lucide Icons** - Modern icon set
- **SCSS** - Enhanced CSS styling
- **SignalR Client** - WebSocket client library
- **WebRTC** - Peer-to-peer video/audio calling

## 📋 Prerequisites

- **.NET 9.0 SDK** or later
- **Node.js 18+** and npm
- **Angular CLI 19+**
- **Git**

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone <repository-url>
cd testCursor
```

### 2. Backend Setup

```bash
# Navigate to API project
cd SocialMediaApp.Api

# Restore dependencies
dotnet restore

# Run database migrations
dotnet ef database update

# Run the API
dotnet run
```

The API will start at `https://localhost:5014`

### 3. Frontend Setup

```bash
# Navigate to client project
cd social-media-app-client

# Install dependencies
npm install

# Start the development server
npm start
```

The application will open at `http://localhost:4200`

## 🔌 API Endpoints

### Authentication
```http
POST   /api/auth/register        # Register new user
POST   /api/auth/login           # Login user
```

### Posts & Feed
```http
GET    /api/posts                # Get user feed (paginated)
POST   /api/posts                # Create new post
POST   /api/posts/{id}/like      # Like a post
DELETE /api/posts/{id}/like      # Unlike a post
```

### Comments
```http
GET    /api/posts/{postId}/comments           # Get post comments (paginated)
POST   /api/posts/{postId}/comments           # Add comment
DELETE /api/posts/{postId}/comments/{commentId} # Delete comment
```

### Users
```http
GET    /api/users/search         # Search users by query
POST   /api/users/{id}/follow    # Follow user
DELETE /api/users/{id}/follow    # Unfollow user
```

### Chat & Messaging
```http
GET    /api/chats                        # Get user's chat list
GET    /api/chats/{chatId}/messages     # Get chat messages
POST   /api/chats                        # Send message
POST   /api/chats/{chatId}/read         # Mark messages as read
POST   /api/chats/create/{userId}       # Get or create chat with user
```

### SignalR Hubs
```
Chat Hub - WebSocket: /hubs/chat
Methods:
- SendMessage(receiverId, content)       # Send real-time message
- MarkAsRead(chatId)                     # Mark messages as read
Events:
- ReceiveMessage(message)                # Receive new message
- MessagesMarkedAsRead(chatId)           # Messages marked as read

Video Hub - WebSocket: /hubs/video
Methods:
- SendOffer(receiverId, offerSdp)        # Initiate video call
- SendAnswer(callerId, answerSdp)        # Accept video call
- SendCandidate(userId, candidateJson)   # Exchange ICE candidates
- HangupCall(remoteUserId)               # End call
Events:
- ReceiveOffer(callerId, callerName, sdp) # Incoming call
- ReceiveAnswer(answerSdp)               # Call accepted
- ReceiveCandidate(candidateJson)        # ICE candidate received
- ReceiveHangup()                        # Call ended
```

## 📊 Database Schema

### Core Entities

**Users**
- Id (Guid, PK)
- UserName (unique)
- Email (unique)
- DisplayName
- Bio
- PasswordHash
- ProfileImageUrl
- CreatedAtUtc

**Posts**
- Id (Guid, PK)
- UserId (FK → Users)
- Content
- ImageUrl
- CreatedAtUtc

**Comments**
- Id (Guid, PK)
- PostId (FK → Posts)
- UserId (FK → Users)
- Content
- CreatedAtUtc

**Likes**
- Id (Guid, PK)
- PostId (FK → Posts)
- UserId (FK → Users)
- CreatedAtUtc

**UserFollowers**
- Id (Guid, PK)
- FollowerId (FK → Users)
- FollowedId (FK → Users)
- CreatedAtUtc

**Chats**
- Id (Guid, PK)
- User1Id (FK → Users)
- User2Id (FK → Users)
- LastMessageAtUtc
- CreatedAtUtc

**Messages**
- Id (Guid, PK)
- ChatId (FK → Chats)
- SenderId (FK → Users)
- Content
- IsRead
- CreatedAtUtc

## 🎨 Key Components

### Frontend Components

#### AuthGuard
Protects routes requiring authentication. Redirects unauthenticated users to login.

#### FeedPageComponent
- Displays personalized feed based on followed users
- Infinite scroll with pagination
- Create posts with optional images
- Like/unlike posts with optimistic updates
- View and add comments
- Delete own comments

#### UserDiscoveryComponent
- Search users with debounced input
- Real-time search results
- Follow/unfollow functionality
- Direct messaging button
- User statistics display

#### ChatPageComponent
- Chat list sidebar with unread counts
- Real-time message updates via SignalR
- Message composition and sending
- Read receipts
- Auto-scroll to latest messages
- Video call button integration
- Responsive design

#### VideoCallComponent
- Full-screen video interface
- Local and remote video streams
- Audio/video toggle controls
- Call duration timer
- Hang up functionality
- Glassmorphism design with animations
- Mobile-responsive layout

#### IncomingCallModalComponent
- Full-screen incoming call notification
- Caller information display
- Accept/Decline actions
- Auto-decline after 30 seconds
- Animated caller avatar
- Mobile-optimized design

#### NavbarComponent
- Navigation links (Feed, Discover, Messages)
- User profile display
- Logout functionality

### Backend Services

#### AuthService
- User registration with password hashing
- JWT token generation
- User authentication

#### PostService
- Create posts
- Get personalized feed based on following
- Like/unlike posts
- Post statistics (like count, comment count)

#### CommentService
- Add comments to posts
- Get paginated comments
- Delete own comments
- User validation

#### UserService
- Search users by name/username
- Follow/unfollow functionality
- User statistics

#### ChatService
- Create or get existing chats
- Send messages
- Get chat history
- Mark messages as read
- Unread message counting

### Frontend Services

#### SignalRService
- Centralized SignalR connection management
- Multiple hub support (chat, video)
- Connection status monitoring per hub
- Automatic reconnection with exponential backoff
- Event handler registration and cleanup
- Token-based authentication

#### WebRTCService
- WebRTC peer connection management
- Offer/answer negotiation
- ICE candidate exchange
- Media stream acquisition (audio/video)
- Track state management (enable/disable audio/video)
- Echo cancellation and noise suppression
- Bandwidth optimization via SDP manipulation
- Call state management (idle, ringing, connected, etc.)

## 🔒 Security Features

- **JWT Authentication**: Secure token-based authentication
- **Password Hashing**: Bcrypt-based password security
- **Authorization**: All endpoints require valid JWT tokens
- **CORS Configuration**: Properly configured for development
- **Input Validation**: Server-side validation for all inputs
- **SQL Injection Prevention**: Parameterized queries via EF Core

## 🌐 Real-time Features

### SignalR Integration

The application uses SignalR for real-time bidirectional communication:

1. **Connection Management**: Automatic connection with JWT authentication
2. **User Groups**: Each user joins their own group for targeted messaging
3. **Automatic Reconnection**: Handles disconnections gracefully
4. **Event Broadcasting**: Real-time message delivery to both sender and receiver

### Client-Side Implementation
```typescript
// SignalR connection with JWT
const hubUrl = `${apiUrl}/hubs/chat?access_token=${token}`;
const connection = new HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect()
  .build();

// Listen for messages
connection.on('ReceiveMessage', (message) => {
  // Handle incoming message
});

// Send message
connection.invoke('SendMessage', receiverId, content);
```

## 🎯 Usage Guide

### Getting Started
1. **Register**: Create a new account with username, email, and password
2. **Login**: Sign in with your credentials
3. **Discover Users**: Navigate to the Discover page to find users
4. **Follow Users**: Click "Follow" on user profiles
5. **View Feed**: See posts from users you follow on the Feed page
6. **Create Posts**: Share your thoughts with optional images
7. **Engage**: Like and comment on posts
8. **Message**: Click "Message" on any user profile to start chatting

### Creating Content
- **Posts**: Click "Share your thoughts..." on the Feed page
- **Comments**: Click the comment icon and type your comment
- **Messages**: Use the Messages page or click "Message" on user profiles

## 🔧 Configuration

### Backend Configuration (appsettings.json)
```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters",
    "Issuer": "SocialMediaApp",
    "Audience": "SocialMediaApp",
    "ExpirationMinutes": 1440
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

**Note**: Redis configuration is **optional** and only needed for horizontal scaling (multiple API servers). In single-server deployments, SignalR works perfectly without Redis.

### Frontend Configuration (environment.ts)
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5014/api'
};
```

## 📝 Development Notes

### Adding New Features
1. **Backend**: Create endpoint → Add service method → Update repository
2. **Frontend**: Create service method → Update component → Add to template

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName --project SocialMediaApp.Infrastructure

# Apply migration
dotnet ef database update --project SocialMediaApp.Api

# Revert migration
dotnet ef database update PreviousMigrationName --project SocialMediaApp.Api
```

### Code Style
- Backend: Follow C# naming conventions (PascalCase for public members)
- Frontend: Follow Angular style guide (camelCase, use signals)
- Use async/await for asynchronous operations
- Implement proper error handling

## 🐛 Troubleshooting

### Backend Issues
**Port Already in Use**
```bash
# Kill process on port 5014
netstat -ano | findstr :5014
taskkill /PID <process-id> /F
```

**Migration Errors**
```bash
# Drop database and recreate
dotnet ef database drop --project SocialMediaApp.Api
dotnet ef database update --project SocialMediaApp.Api
```

### Frontend Issues
**Port 4200 in Use**
```bash
# The Angular CLI will automatically prompt to use a different port
```

**Module Not Found**
```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

## 📚 Additional Resources

### Project Documentation
- [API Reference](API.md) - Complete API endpoint documentation
- [Architecture Guide](ARCHITECTURE.md) - Technical architecture details
- [Messaging System](MESSAGING.md) - Hybrid messaging implementation guide

### External Resources
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Angular Documentation](https://angular.dev)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

## 🤝 Contributing

Contributions are welcome! Please follow these steps:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 👥 Authors

Built with ❤️ using .NET and Angular

---

**Note**: This is a development build. For production deployment:
- Use proper environment variables
- Configure production database (SQL Server/PostgreSQL)
- Enable HTTPS
- Set up proper CORS policies
- Use secure JWT secrets
- Implement rate limiting
- Add comprehensive logging
- Set up CI/CD pipelines
- **Configure Redis backplane for SignalR** when deploying to multiple servers (load balanced environments)

