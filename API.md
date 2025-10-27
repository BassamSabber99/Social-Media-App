# API Documentation

Complete reference for all REST API endpoints and SignalR hubs.

## Base URL

```
Development: https://localhost:5014/api
Production: https://your-domain.com/api
```

## Authentication

All endpoints except `/auth/login` and `/auth/register` require authentication using JWT Bearer tokens.

### Headers

```http
Authorization: Bearer {jwt_token}
Content-Type: application/json
Accept: application/json
```

---

## Authentication Endpoints

### Register User

Creates a new user account.

```http
POST /auth/register
```

**Request Body:**
```json
{
  "userName": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe"
}
```

**Error Responses:**
- `400 Bad Request` - Validation failed
- `409 Conflict` - Username or email already exists

---

### Login

Authenticates a user and returns a JWT token.

```http
POST /auth/login
```

**Request Body:**
```json
{
  "userName": "johndoe",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userName": "johndoe",
  "email": "john@example.com",
  "displayName": "John Doe"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid credentials
- `401 Unauthorized` - Username or password incorrect

---

## Post Endpoints

### Get Feed

Retrieves personalized feed based on followed users.

```http
GET /posts?skip=0&take=20
```

**Query Parameters:**
- `skip` (optional): Number of posts to skip (default: 0)
- `take` (optional): Number of posts to return (default: 20, max: 50)

**Response:** `200 OK`
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "content": "Hello World! This is my first post.",
    "imageUrl": "https://example.com/image.jpg",
    "author": {
      "id": "user-id",
      "userName": "johndoe",
      "displayName": "John Doe",
      "profileImageUrl": null
    },
    "likesCount": 15,
    "commentsCount": 3,
    "isLikedByRequester": true,
    "createdAtUtc": "2025-10-05T12:00:00Z"
  }
]
```

**Response Headers:**
```
X-Total-Count: 45
```

---

### Create Post

Creates a new post.

```http
POST /posts
```

**Request Body:**
```json
{
  "content": "Hello World! This is my first post.",
  "imageUrl": "https://example.com/image.jpg"  // optional
}
```

**Response:** `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Hello World! This is my first post.",
  "imageUrl": "https://example.com/image.jpg",
  "author": {
    "id": "user-id",
    "userName": "johndoe",
    "displayName": "John Doe",
    "profileImageUrl": null
  },
  "likesCount": 0,
  "commentsCount": 0,
  "isLikedByRequester": false,
  "createdAtUtc": "2025-10-05T12:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Content is required
- `401 Unauthorized` - Not authenticated

---

### Like Post

Likes a post.

```http
POST /posts/{postId}/like
```

**Path Parameters:**
- `postId`: GUID of the post

**Response:** `200 OK`

**Error Responses:**
- `401 Unauthorized` - Not authenticated
- `404 Not Found` - Post not found

---

### Unlike Post

Removes like from a post.

```http
DELETE /posts/{postId}/like
```

**Path Parameters:**
- `postId`: GUID of the post

**Response:** `200 OK`

---

## Comment Endpoints

### Get Comments

Retrieves comments for a specific post.

```http
GET /posts/{postId}/comments?skip=0&take=20
```

**Path Parameters:**
- `postId`: GUID of the post

**Query Parameters:**
- `skip` (optional): Number of comments to skip
- `take` (optional): Number of comments to return

**Response:** `200 OK`
```json
[
  {
    "id": "comment-id",
    "postId": "post-id",
    "content": "Great post!",
    "author": {
      "id": "user-id",
      "userName": "janedoe",
      "displayName": "Jane Doe",
      "profileImageUrl": null
    },
    "createdAtUtc": "2025-10-05T12:30:00Z"
  }
]
```

**Response Headers:**
```
X-Total-Count: 12
```

---

### Add Comment

Adds a comment to a post.

```http
POST /posts/{postId}/comments
```

**Path Parameters:**
- `postId`: GUID of the post

**Request Body:**
```json
{
  "content": "Great post!"
}
```

**Response:** `201 Created`
```json
{
  "id": "comment-id",
  "postId": "post-id",
  "content": "Great post!",
  "author": {
    "id": "user-id",
    "userName": "janedoe",
    "displayName": "Jane Doe",
    "profileImageUrl": null
  },
  "createdAtUtc": "2025-10-05T12:30:00Z"
}
```

---

### Delete Comment

Deletes a comment (only by comment author).

```http
DELETE /posts/{postId}/comments/{commentId}
```

**Path Parameters:**
- `postId`: GUID of the post
- `commentId`: GUID of the comment

**Response:** `204 No Content`

**Error Responses:**
- `403 Forbidden` - Not the comment author
- `404 Not Found` - Comment not found

---

## User Endpoints

### Search Users

Searches for users by username or display name.

```http
GET /users/search?query=john&skip=0&take=20
```

**Query Parameters:**
- `query` (required): Search term (minimum 2 characters)
- `skip` (optional): Number of results to skip
- `take` (optional): Number of results to return

**Response:** `200 OK`
```json
[
  {
    "id": "user-id",
    "userName": "johndoe",
    "email": "john@example.com",
    "displayName": "John Doe",
    "bio": "Software developer and coffee enthusiast",
    "profileImageUrl": "https://example.com/avatar.jpg",
    "followersCount": 120,
    "followingCount": 85,
    "isFollowedByRequester": false,
    "createdAtUtc": "2025-01-01T00:00:00Z"
  }
]
```

---

### Follow User

Follows a user.

```http
POST /users/{userId}/follow
```

**Path Parameters:**
- `userId`: GUID of the user to follow

**Response:** `200 OK`

**Error Responses:**
- `400 Bad Request` - Cannot follow yourself
- `409 Conflict` - Already following

---

### Unfollow User

Unfollows a user.

```http
DELETE /users/{userId}/follow
```

**Path Parameters:**
- `userId`: GUID of the user to unfollow

**Response:** `200 OK`

---

## Chat Endpoints

### Get User Chats

Retrieves all chat conversations for the current user.

```http
GET /chats
```

**Response:** `200 OK`
```json
[
  {
    "id": "chat-id",
    "otherUserId": "user-id",
    "otherUserName": "johndoe",
    "otherUserDisplayName": "John Doe",
    "otherUserProfileImageUrl": null,
    "lastMessageAtUtc": "2025-10-05T14:30:00Z",
    "lastMessageContent": "Hey, how are you?",
    "unreadCount": 2
  }
]
```

---

### Get Chat Messages

Retrieves messages for a specific chat.

```http
GET /chats/{chatId}/messages?skip=0&take=50
```

**Path Parameters:**
- `chatId`: GUID of the chat

**Query Parameters:**
- `skip` (optional): Number of messages to skip
- `take` (optional): Number of messages to return

**Response:** `200 OK`
```json
[
  {
    "id": "message-id",
    "chatId": "chat-id",
    "senderId": "sender-id",
    "senderUserName": "johndoe",
    "senderDisplayName": "John Doe",
    "content": "Hey, how are you?",
    "isRead": true,
    "createdAtUtc": "2025-10-05T14:30:00Z"
  }
]
```

**Response Headers:**
```
X-Total-Count: 45
```

---

### Send Message

Sends a message via HTTP API.

> **Note**: This is the **fallback method**. For real-time messaging, use SignalR `SendMessage` hub method. The application automatically uses SignalR when connected and falls back to this HTTP endpoint when SignalR is unavailable.

```http
POST /chats
```

**Request Body:**
```json
{
  "receiverId": "receiver-user-id",
  "content": "Hey, how are you?"
}
```

**Response:** `200 OK`
```json
{
  "id": "message-id",
  "chatId": "chat-id",
  "senderId": "sender-id",
  "senderUserName": "johndoe",
  "senderDisplayName": "John Doe",
  "content": "Hey, how are you?",
  "isRead": false,
  "createdAtUtc": "2025-10-05T14:30:00Z"
}
```

---

### Mark Messages as Read

Marks all messages in a chat as read.

```http
POST /chats/{chatId}/read
```

**Path Parameters:**
- `chatId`: GUID of the chat

**Response:** `200 OK`

---

### Get or Create Chat

Gets existing chat or creates a new one with a user.

```http
POST /chats/create/{userId}
```

**Path Parameters:**
- `userId`: GUID of the user to chat with

**Response:** `200 OK`
```json
{
  "chatId": "chat-id"
}
```

**Error Responses:**
- `404 Not Found` - User not found

---

## SignalR Hubs

The application uses **two SignalR hubs** for real-time communication:
1. **Chat Hub** (`/hubs/chat`) - Text messaging
2. **Video Hub** (`/hubs/video`) - Video call signaling

### Chat Hub Connection

```javascript
WebSocket: wss://localhost:5014/hubs/chat
```

**Hybrid Messaging Architecture:**
The application implements a **hybrid approach** for reliable messaging:
- **Primary**: SignalR WebSocket for real-time delivery
- **Fallback**: HTTP REST API for guaranteed delivery
- **Automatic**: Seamlessly switches between methods based on connection status

**Authentication:**
```javascript
const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:5014/hubs/chat?access_token=YOUR_JWT_TOKEN')
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
      // Exponential backoff: 0s, 2s, 10s, 30s, max 60s
      if (retryContext.previousRetryCount === 0) return 0;
      if (retryContext.previousRetryCount === 1) return 2000;
      if (retryContext.previousRetryCount === 2) return 10000;
      if (retryContext.previousRetryCount === 3) return 30000;
      return 60000;
    }
  })
  .build();

// Connection status handlers
connection.onclose(() => console.log('Disconnected - using HTTP fallback'));
connection.onreconnecting(() => console.log('Reconnecting...'));
connection.onreconnected(() => console.log('Reconnected - real-time enabled'));
```

---

### Hub Methods (Client → Server)

#### SendMessage

Sends a real-time message via WebSocket.

```javascript
// Basic usage (no error handling)
await connection.invoke('SendMessage', receiverId, content);

// Recommended: With error handling and HTTP fallback
try {
  await connection.invoke('SendMessage', receiverId, content);
  console.log('Message sent via SignalR');
} catch (error) {
  console.warn('SignalR failed, falling back to HTTP');
  const response = await fetch('/api/chats', {
    method: 'POST',
    headers: { 
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ receiverId, content })
  });
  const message = await response.json();
  // Manually add message to UI
}
```

**Parameters:**
- `receiverId`: GUID - The user to send the message to
- `content`: string - The message content

**Returns:** Promise<void>

**Throws:** Error if not connected or hub method fails

---

#### MarkAsRead

Marks messages in a chat as read.

```javascript
await connection.invoke('MarkAsRead', chatId);
```

**Parameters:**
- `chatId`: GUID - The chat to mark as read

**Returns:** void

---

### Hub Events (Server → Client)

#### ReceiveMessage

Fired when a new message is received.

```javascript
connection.on('ReceiveMessage', (message) => {
  console.log('New message:', message);
});
```

**Message Object:**
```typescript
{
  id: string;
  chatId: string;
  senderId: string;
  senderUserName: string;
  senderDisplayName: string;
  content: string;
  isRead: boolean;
  createdAtUtc: Date;
}
```

---

#### MessagesMarkedAsRead

Fired when messages are marked as read.

```javascript
connection.on('MessagesMarkedAsRead', (chatId) => {
  console.log('Messages read in chat:', chatId);
});
```

**Parameters:**
- `chatId`: string - The chat where messages were marked as read

---

## Video Hub Connection

```javascript
WebSocket: wss://localhost:5014/hubs/video
```

**WebRTC Video Calling:**
The application implements **peer-to-peer video calling** using WebRTC with SignalR for signaling:
- **Signaling**: SignalR handles connection negotiation (offer/answer/ICE candidates)
- **Media**: WebRTC establishes direct peer-to-peer audio/video streams
- **Quality**: Optimized bandwidth allocation (audio 64kbps, video 512kbps)
- **Features**: Audio/video toggle, echo cancellation, noise suppression

**Authentication:**
```javascript
const connection = new HubConnectionBuilder()
  .withUrl('https://localhost:5014/hubs/video?access_token=YOUR_JWT_TOKEN')
  .withAutomaticReconnect()
  .build();
```

---

### Hub Methods (Client → Server)

#### SendOffer

Initiates a video call by sending a WebRTC offer to another user.

```javascript
await connection.invoke('SendOffer', receiverId, offerSdp);
```

**Parameters:**
- `receiverId`: GUID - The user to call
- `offerSdp`: string - JSON-stringified RTCSessionDescription (offer)

**Returns:** void

**Workflow:**
1. Caller creates peer connection
2. Caller generates offer SDP
3. Sends offer to callee via SignalR
4. Callee receives `ReceiveOffer` event

---

#### SendAnswer

Responds to an incoming video call with a WebRTC answer.

```javascript
await connection.invoke('SendAnswer', callerId, answerSdp);
```

**Parameters:**
- `callerId`: GUID - The user who initiated the call
- `answerSdp`: string - JSON-stringified RTCSessionDescription (answer)

**Returns:** void

**Workflow:**
1. Callee receives offer
2. Callee creates peer connection
3. Callee generates answer SDP
4. Sends answer to caller via SignalR
5. Caller receives `ReceiveAnswer` event

---

#### SendCandidate

Exchanges ICE candidates for NAT traversal during connection setup.

```javascript
await connection.invoke('SendCandidate', targetUserId, candidateJson);
```

**Parameters:**
- `targetUserId`: GUID - The other user in the call
- `candidateJson`: string - JSON-stringified RTCIceCandidate

**Returns:** void

**Note**: ICE candidates are discovered automatically during peer connection setup and must be exchanged to establish the media connection.

---

#### HangupCall

Terminates an active video call.

```javascript
await connection.invoke('HangupCall', remoteUserId);
```

**Parameters:**
- `remoteUserId`: GUID - The other user in the call

**Returns:** void

---

### Hub Events (Server → Client)

#### ReceiveOffer

Fired when another user initiates a video call.

```javascript
connection.on('ReceiveOffer', (callerId, callerName, offerSdp) => {
  // Show incoming call UI
  // Display caller name
  // Allow user to accept/decline
});
```

**Parameters:**
- `callerId`: string (GUID) - ID of the calling user
- `callerName`: string - Display name of the caller
- `offerSdp`: string - JSON-stringified offer to be set as remote description

**Client Actions:**
- Display incoming call modal
- If accepted: Call `SendAnswer` with local answer SDP
- If declined: Call `HangupCall`

---

#### ReceiveAnswer

Fired when the callee accepts the call and sends their answer.

```javascript
connection.on('ReceiveAnswer', (answerSdp) => {
  // Set remote description with answer
  // Connection will complete
});
```

**Parameters:**
- `answerSdp`: string - JSON-stringified answer to be set as remote description

---

#### ReceiveCandidate

Fired when an ICE candidate is received from the remote peer.

```javascript
connection.on('ReceiveCandidate', (candidateJson) => {
  const candidate = JSON.parse(candidateJson);
  await peerConnection.addIceCandidate(candidate);
});
```

**Parameters:**
- `candidateJson`: string - JSON-stringified ICE candidate

**Note**: Candidates should be queued if received before remote description is set.

---

#### ReceiveHangup

Fired when the remote user ends the call.

```javascript
connection.on('ReceiveHangup', () => {
  // Close peer connection
  // Stop media streams
  // Return to idle state
});
```

**Parameters:** None

---

### WebRTC Connection Flow

```
User A (Caller)                         SignalR Hub                         User B (Callee)
      │                                        │                                    │
      ├─────── SendOffer ────────────────────>│                                    │
      │        (offer SDP)                     │                                    │
      │                                        ├─────── ReceiveOffer ──────────────>│
      │                                        │        (callerId, callerName, SDP) │
      │                                        │                                    │
      │                                        │                                    │ [Show incoming call]
      │                                        │                                    │ [User accepts]
      │                                        │                                    │
      │                                        │<────── SendAnswer ─────────────────┤
      │                                        │        (answer SDP)                │
      │<────── ReceiveAnswer ──────────────────┤                                    │
      │        (answer SDP)                    │                                    │
      │                                        │                                    │
      │ [Exchange ICE Candidates]              │ [Relay ICE Candidates]            │
      ├─────── SendCandidate ─────────────────>├─────── ReceiveCandidate ──────────>│
      │<────── ReceiveCandidate ────────────────┤<────── SendCandidate ─────────────┤
      │                                        │                                    │
      │ [WebRTC Peer Connection Established]   │                                    │
      │◄═══════════════════════════════════════════════════════════════════════════►│
      │                  Audio/Video Streams (Direct P2P)                          │
      │                                        │                                    │
      ├─────── HangupCall ────────────────────>│                                    │
      │                                        ├─────── ReceiveHangup ─────────────>│
      │                                        │                                    │
```

---

### Media Constraints

The application uses optimized media constraints for quality and bandwidth:

**Audio:**
```javascript
{
  echoCancellation: { ideal: true },
  noiseSuppression: { ideal: true },
  autoGainControl: { ideal: true },
  sampleRate: { ideal: 48000 },
  channelCount: { ideal: 1 }  // Mono for stability
}
```

**Video:**
```javascript
{
  width: { ideal: 640, max: 1280 },
  height: { ideal: 480, max: 720 },
  frameRate: { ideal: 24, max: 30 }
}
```

**Bandwidth Allocation (SDP):**
- Audio: 64 kbps (prioritized for clarity)
- Video: 512 kbps (limited to ensure audio quality)

---

### Horizontal Scaling with Redis Backplane

For **single-server deployments** (most common), SignalR works perfectly without any additional configuration.

For **multi-server deployments** (load-balanced environments), configure Redis backplane to synchronize SignalR messages across servers:

#### Configuration

**appsettings.json:**
```json
{
  "Redis": {
    "ConnectionString": "your-redis-server:6379,password=yourpassword"
  }
}
```

**When Redis is configured:**
- All SignalR messages are broadcast through Redis
- Users can be on different API servers and still receive real-time messages
- Automatic failover if one server goes down
- Seamless server scaling without dropped connections

**When Redis is NOT configured:**
- SignalR runs in single-server mode (default)
- Perfect for development and small deployments
- No external dependencies required

#### Examples

**Azure Redis:**
```json
"Redis": {
  "ConnectionString": "your-app.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False"
}
```

**AWS ElastiCache:**
```json
"Redis": {
  "ConnectionString": "your-cluster.cache.amazonaws.com:6379"
}
```

**Docker Compose:**
```yaml
services:
  redis:
    image: redis:alpine
    ports:
      - "6379:6379"
  
  api:
    environment:
      - Redis__ConnectionString=redis:6379
```

**Note**: The HTTP fallback mechanism ensures reliability even without Redis. Redis is ONLY needed for scaling SignalR across multiple servers, not for basic messaging reliability.

---

## Error Responses

### Standard Error Format

```json
{
  "error": "Error message description"
}
```

### HTTP Status Codes

- `200 OK` - Request succeeded
- `201 Created` - Resource created successfully
- `204 No Content` - Request succeeded with no content to return
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required or failed
- `403 Forbidden` - Authenticated but not authorized
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict (e.g., duplicate)
- `500 Internal Server Error` - Server error

---

## Rate Limiting

Currently no rate limiting is implemented. For production deployment, consider implementing:
- API rate limiting (e.g., 100 requests per minute per user)
- SignalR connection limits
- Message send rate limits

---

## Pagination

All list endpoints support pagination using `skip` and `take` parameters:

```http
GET /posts?skip=20&take=20  # Get posts 21-40
```

**Response Headers:**
- `X-Total-Count`: Total number of items available

---

## Versioning

Currently API v1. Future versions will be handled via:
- URL versioning: `/api/v2/posts`
- Header versioning: `Accept: application/vnd.socialmedia.v2+json`

---

## CORS

CORS is configured for development. For production:
- Specify allowed origins
- Configure allowed methods
- Set appropriate credentials policy

---

## Messaging: When to Use SignalR vs HTTP

### Decision Flow

```
Is user in chat page?
├─ YES
│  └─ Is SignalR connected?
│     ├─ YES → Use SignalR (instant delivery)
│     └─ NO → Use HTTP (guaranteed delivery)
└─ NO → Use HTTP (no WebSocket needed)
```

### Use Cases

**Use SignalR When:**
- ✅ User is actively in the chat page
- ✅ Real-time delivery is important
- ✅ WebSocket connection is established
- ✅ Both users need instant updates

**Use HTTP API When:**
- ✅ SignalR connection failed or unavailable
- ✅ Background/automated messages
- ✅ Older browser without WebSocket support
- ✅ Need synchronous response with message object
- ✅ Guaranteed delivery is more important than speed

### Hybrid Implementation (Recommended)

```typescript
async sendMessage(receiverId: string, content: string) {
  try {
    // Try SignalR first for real-time delivery
    await chatService.sendMessageViaHub(receiverId, content);
  } catch (error) {
    // Automatic fallback to HTTP for reliability
    const message = await chatService.sendMessage({ receiverId, content });
    // Manually add to UI since no SignalR event
    this.messages.push(message);
  }
}
```

---

## Examples

### Complete User Flow Example

```javascript
// 1. Register
const registerResponse = await fetch('https://localhost:5014/api/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    userName: 'newuser',
    email: 'new@example.com',
    displayName: 'New User',
    password: 'Password123!'
  })
});
const { token } = await registerResponse.json();

// 2. Get Feed
const feedResponse = await fetch('https://localhost:5014/api/posts?skip=0&take=10', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const posts = await feedResponse.json();

// 3. Create Post
await fetch('https://localhost:5014/api/posts', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    content: 'My first post!',
    imageUrl: null
  })
});

// 4. Connect to SignalR
const connection = new signalR.HubConnectionBuilder()
  .withUrl(`https://localhost:5014/hubs/chat?access_token=${token}`)
  .build();

connection.on('ReceiveMessage', (message) => {
  console.log('New message:', message);
});

await connection.start();

// 5. Send Message
await connection.invoke('SendMessage', 'receiver-user-id', 'Hello!');
```

---

## Testing with cURL

### Register User
```bash
curl -X POST https://localhost:5014/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"userName":"testuser","email":"test@example.com","displayName":"Test User","password":"Test123!"}'
```

### Login
```bash
curl -X POST https://localhost:5014/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"testuser","password":"Test123!"}'
```

### Get Feed
```bash
curl -X GET https://localhost:5014/api/posts \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Create Post
```bash
curl -X POST https://localhost:5014/api/posts \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"content":"Test post","imageUrl":null}'
```

---

For more information, see the main [README.md](README.md) and [ARCHITECTURE.md](ARCHITECTURE.md) files.

