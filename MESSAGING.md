# Messaging System Documentation

## Overview

The application implements a **hybrid messaging system** that combines SignalR WebSockets for real-time communication with HTTP REST API as a reliable fallback mechanism. This ensures messages are delivered even during network instability or connection issues.

## Architecture

### Dual Approach

1. **Primary: SignalR WebSockets** (Real-time)
   - Instant bidirectional communication
   - Low latency messaging
   - Both users receive updates immediately
   - Persistent connection with automatic reconnection

2. **Fallback: HTTP REST API** (Reliable)
   - Works without WebSocket support
   - Guaranteed delivery via database persistence
   - Returns message object synchronously
   - No persistent connection required

## Implementation

### ChatService Methods

#### Connection Management

```typescript
// Check if SignalR is connected
isConnected(): boolean

// Get current connection status
getConnectionStatus(): string

// Observable for connection status changes
connectionStatus$: Observable<boolean>
```

#### Sending Messages

**SignalR Method (Primary)**
```typescript
sendMessageViaHub(receiverId: string, content: string): Promise<void>
```
- Requires active SignalR connection
- Instant delivery to both users
- Returns Promise for error handling
- Rejects if not connected

**HTTP Method (Fallback)**
```typescript
sendMessage(request: SendMessageRequest): Observable<MessageDto>
```
- Works without SignalR connection
- Returns the saved message object
- Sender must manually add to UI
- Receiver won't see it until refresh

**Hybrid Method (Recommended)**
```typescript
sendMessageWithFallback(receiverId: string, content: string): Promise<MessageDto | void>
```
- Tries SignalR first
- Automatically falls back to HTTP if SignalR fails
- Returns message object only if HTTP was used
- Handles all error scenarios

### Component Implementation

```typescript
async sendMessage(): Promise<void> {
  const content = this.messageContent().trim();
  const chat = this.selectedChat();
  
  if (!content || !chat || this.sending()) return;

  this.messageContent.set('');
  this.sending.set(true);
  
  try {
    const result = await this.chatService.sendMessageWithFallback(
      chat.otherUserId, 
      content
    );
    
    // If HTTP fallback was used, manually add message to UI
    if (result) {
      this.messages.update(msgs => [...msgs, result]);
      setTimeout(() => this.scrollToBottom(), 100);
    }
  } catch (error) {
    console.error('Failed to send message:', error);
    this.messageContent.set(content); // Restore on error
  } finally {
    this.sending.set(false);
  }
}
```

## Connection Management

### Automatic Reconnection

The system implements **exponential backoff** for reconnection attempts:

```typescript
withAutomaticReconnect({
  nextRetryDelayInMilliseconds: (retryContext) => {
    if (retryContext.previousRetryCount === 0) return 0;      // Immediate
    if (retryContext.previousRetryCount === 1) return 2000;   // 2 seconds
    if (retryContext.previousRetryCount === 2) return 10000;  // 10 seconds
    if (retryContext.previousRetryCount === 3) return 30000;  // 30 seconds
    return 60000;                                              // 60 seconds (max)
  }
})
```

### Connection Events

**onclose**: Connection closed
- Updates connection status to disconnected
- Triggers visual indicator change
- Future messages use HTTP fallback

**onreconnecting**: Attempting to reconnect
- Shows "reconnecting" state
- Applies exponential backoff delays

**onreconnected**: Successfully reconnected
- Updates connection status to connected
- Future messages use SignalR again

### Connection Status Indicator

Visual indicator shows connection state:

**Real-time Mode** (Connected)
- ✅ Green pulsing dot
- "Real-time" label
- Messages sent via SignalR

**Offline Mode** (Disconnected)
- ❌ Red static dot
- "Offline mode" label
- Messages sent via HTTP

## Message Flow Scenarios

### Scenario 1: Normal Operation (SignalR Connected)

```
1. User A types message
2. Click Send
3. ChatService.sendMessageWithFallback()
4. Checks: isConnected() → true
5. Calls: sendMessageViaHub()
6. SignalR Hub: SendMessage()
7. Server saves to database
8. Hub broadcasts: ReceiveMessage event
9. User A receives via event (no UI update needed)
10. User B receives via event (instant update)
```

**Result**: Instant delivery, both users see message immediately

---

### Scenario 2: SignalR Disconnected

```
1. User A types message
2. Click Send
3. ChatService.sendMessageWithFallback()
4. Checks: isConnected() → false (OR SignalR fails)
5. Catches error, falls back to HTTP
6. POST /api/chats
7. Server saves to database
8. Returns MessageDto
9. User A manually adds to UI
10. User B doesn't receive notification
```

**Result**: Message saved, sender sees it, receiver needs to refresh

---

### Scenario 3: Connection Drops Mid-Send

```
1. User A types message
2. Click Send
3. ChatService.sendMessageWithFallback()
4. Checks: isConnected() → true
5. Calls: sendMessageViaHub()
6. Network drops during invoke()
7. Promise rejects with error
8. Catches error, falls back to HTTP
9. POST /api/chats
10. Returns MessageDto
11. User A manually adds to UI
```

**Result**: Automatic fallback ensures delivery

---

### Scenario 4: Reconnection During Chat

```
1. SignalR disconnected (network issues)
2. Status indicator shows "Offline mode"
3. User sends messages via HTTP
4. Network restored
5. SignalR: onreconnecting event
6. SignalR: exponential backoff retry
7. SignalR: onreconnected event
8. Status indicator shows "Real-time"
9. Future messages use SignalR again
```

**Result**: Seamless transition between modes

## When to Use Each Method

### Use SignalR (sendMessageViaHub / sendMessageWithFallback)

✅ **Always use the hybrid approach** in production
- Automatically handles both scenarios
- No need to check connection manually
- Provides best user experience

```typescript
// ✅ Recommended
await this.chatService.sendMessageWithFallback(userId, content);
```

### Use HTTP API Directly (sendMessage)

Use only when:
- Building features that don't need real-time (notifications, logs)
- Testing the API independently
- SignalR is intentionally disabled
- You want explicit control over the flow

```typescript
// ⚠️ Use sparingly - no real-time
this.chatService.sendMessage({ receiverId, content }).subscribe();
```

### Never Use Without Fallback

❌ **Don't do this** in production:
```typescript
// ❌ Bad: No fallback, fails silently if disconnected
this.chatService.sendMessageViaHub(userId, content);
```

## Error Handling

### Message Send Failures

```typescript
try {
  await chatService.sendMessageWithFallback(receiverId, content);
} catch (error) {
  // Both SignalR and HTTP failed
  // Show error to user
  // Optionally queue for retry
  console.error('All messaging methods failed:', error);
}
```

### Connection Status Monitoring

```typescript
chatService.connectionStatus$.subscribe(connected => {
  if (connected) {
    console.log('Real-time messaging available');
  } else {
    console.log('Using reliable HTTP fallback');
  }
});
```

## Testing

### Test SignalR Connection

```typescript
// Check connection
console.log(chatService.isConnected()); // true/false
console.log(chatService.getConnectionStatus()); // 'Connected', 'Reconnecting', etc.
```

### Test Fallback Mechanism

1. Open chat page
2. Open browser DevTools → Network tab
3. Throttle network to "Offline"
4. Send a message
5. Should see HTTP POST request to `/api/chats`
6. Message appears in UI
7. Restore network
8. Status indicator changes to "Real-time"

### Test Automatic Reconnection

1. Open chat page (connected)
2. Open browser DevTools → Network tab
3. Throttle network to "Offline" for 5 seconds
4. Status shows "Offline mode"
5. Restore network
6. Watch console for reconnection attempts
7. Status changes back to "Real-time"

## Performance Considerations

### SignalR (Real-time Mode)
- **Latency**: ~50-100ms
- **Server Load**: Low (persistent connection)
- **Scalability**: Requires backplane for multiple servers (Redis/Azure SignalR)

### HTTP (Fallback Mode)
- **Latency**: ~200-500ms
- **Server Load**: Higher (request per message)
- **Scalability**: Standard HTTP scaling (load balancer)

### Bandwidth
- SignalR: ~500 bytes per message (WebSocket frame)
- HTTP: ~800 bytes per message (HTTP headers + body)

## Production Recommendations

### Configuration

```typescript
// Environment-specific configuration
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com/api',
  signalrUrl: 'https://api.yourdomain.com/hubs',
  enableSignalR: true, // Can disable for testing
  maxReconnectAttempts: 10,
  reconnectBackoff: 'exponential'
};
```

### Monitoring

Track these metrics:
- SignalR connection uptime %
- HTTP fallback usage rate
- Message delivery latency
- Reconnection success rate
- Failed message count

### Load Balancing

For multiple API servers:
- Use sticky sessions (same user → same server)
- OR use SignalR backplane (Redis, Azure SignalR Service)
- HTTP fallback works with any load balancer

### Security

- JWT authentication on both SignalR and HTTP
- Rate limiting on message endpoints
- Message content validation
- XSS protection on message display

## Troubleshooting

### Messages not arriving in real-time

**Check:**
1. Connection status indicator (should show "Real-time")
2. Browser console for SignalR errors
3. Network tab for WebSocket connection
4. Backend logs for Hub errors

**Solution:**
- Messages still work via HTTP fallback
- Check firewall/proxy WebSocket support
- Verify JWT token is valid

### "Offline mode" always showing

**Check:**
1. JWT token is present and valid
2. SignalR Hub URL is correct
3. Server is running and accessible
4. CORS is configured for WebSocket

**Solution:**
- HTTP fallback ensures messages still work
- Check browser console for specific error
- Verify authentication

### Messages sent but not received

**If Sender sees message:**
- Message was saved via HTTP fallback
- Receiver needs to refresh or SignalR reconnect

**If Sender doesn't see message:**
- Both SignalR and HTTP failed
- Check network connectivity
- Check authentication

## Production Deployment: Scaling with Redis

### Single Server vs Multi-Server

**Single Server (Default)**
- ✅ No Redis required
- ✅ SignalR works out of the box
- ✅ Perfect for most applications
- ✅ Simpler deployment

**Multi-Server with Load Balancer**
- 🔧 Requires Redis backplane
- 🔧 Synchronizes SignalR across servers
- 🔧 Supports horizontal scaling
- 🔧 Higher availability

### Redis Backplane Architecture

```
┌─────────────┐
│   Client 1  │────┐
└─────────────┘    │
                   │    ┌──────────────┐      ┌─────────────┐
┌─────────────┐    ├───▶│   API Server │─────▶│             │
│   Client 2  │────┘    │   Instance 1 │      │    Redis    │
└─────────────┘         └──────────────┘      │  Backplane  │
                              │                │             │
┌─────────────┐               │                └─────────────┘
│   Client 3  │────┐          │                      ▲
└─────────────┘    │          └──────────────────────┤
                   │    ┌──────────────┐             │
┌─────────────┐    ├───▶│   API Server │─────────────┘
│   Client 4  │────┘    │   Instance 2 │
└─────────────┘         └──────────────┘
```

- Client 1 sends message to Server 1
- Server 1 broadcasts to Redis
- Redis forwards to Server 2
- Server 2 delivers to Client 3 and 4

### Configuration

#### 1. Local Development (No Redis)

**appsettings.Development.json:**
```json
{
  "Redis": {
    "ConnectionString": ""
  }
}
```

**Behavior:**
- SignalR runs in memory
- Single server mode
- Perfect for development

#### 2. Production with Azure Redis

**appsettings.Production.json:**
```json
{
  "Redis": {
    "ConnectionString": "your-app.redis.cache.windows.net:6380,password=yourkey,ssl=True,abortConnect=False"
  }
}
```

**Setup:**
1. Create Azure Cache for Redis
2. Copy Primary Connection String
3. Add to configuration
4. Deploy multiple API instances

#### 3. Production with AWS ElastiCache

**appsettings.Production.json:**
```json
{
  "Redis": {
    "ConnectionString": "your-cluster.cache.amazonaws.com:6379"
  }
}
```

**Setup:**
1. Create ElastiCache Redis cluster
2. Configure security groups (port 6379)
3. Use cluster endpoint
4. Deploy to ECS/EKS with multiple tasks/pods

#### 4. Docker Compose (Multi-Container)

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    restart: unless-stopped

  api1:
    build: .
    ports:
      - "5001:8080"
    environment:
      - Redis__ConnectionString=redis:6379
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - redis
    restart: unless-stopped

  api2:
    build: .
    ports:
      - "5002:8080"
    environment:
      - Redis__ConnectionString=redis:6379
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - redis
    restart: unless-stopped

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - api1
      - api2
    restart: unless-stopped

volumes:
  redis-data:
```

**nginx.conf (Load Balancer):**
```nginx
events {
    worker_connections 1024;
}

http {
    upstream api_backend {
        ip_hash;  # Sticky sessions for WebSocket
        server api1:8080;
        server api2:8080;
    }

    server {
        listen 80;

        location / {
            proxy_pass http://api_backend;
            proxy_http_version 1.1;
            
            # WebSocket support
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection "upgrade";
            
            # Headers
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            
            # Timeouts
            proxy_connect_timeout 7d;
            proxy_send_timeout 7d;
            proxy_read_timeout 7d;
        }
    }
}
```

#### 5. Kubernetes Deployment

**k8s/redis-deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
      - name: redis
        image: redis:7-alpine
        ports:
        - containerPort: 6379
        volumeMounts:
        - name: redis-data
          mountPath: /data
      volumes:
      - name: redis-data
        persistentVolumeClaim:
          claimName: redis-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: redis
spec:
  selector:
    app: redis
  ports:
  - port: 6379
    targetPort: 6379
```

**k8s/api-deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: social-api
spec:
  replicas: 3  # Multiple API instances
  selector:
    matchLabels:
      app: social-api
  template:
    metadata:
      labels:
        app: social-api
    spec:
      containers:
      - name: api
        image: your-registry/social-media-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: Redis__ConnectionString
          value: "redis:6379"
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
---
apiVersion: v1
kind: Service
metadata:
  name: social-api
spec:
  type: LoadBalancer
  selector:
    app: social-api
  ports:
  - port: 80
    targetPort: 8080
  sessionAffinity: ClientIP  # For WebSocket sticky sessions
```

### Monitoring Redis

**Check Connection:**
```bash
# Connect to Redis CLI
redis-cli -h your-redis-server -p 6379

# Check connected clients
CLIENT LIST

# Monitor real-time commands
MONITOR

# Check SignalR channels
PUBSUB CHANNELS SocialMediaApp*
```

**Expected Output (when SignalR is active):**
```
1) "SocialMediaApp:ack:..."
2) "SocialMediaApp:internal:..."
3) "SocialMediaApp:all"
```

### Performance Considerations

**Redis Sizing:**
- Small apps (< 1000 users): Basic tier (250 MB)
- Medium apps (1-10K users): Standard tier (1 GB)
- Large apps (10K+ users): Premium tier (6+ GB)

**Network:**
- Redis should be in same region/VPC as API servers
- Use private networking when possible
- SSL/TLS for production

**Cost:**
- Azure Redis: ~$15-200/month depending on tier
- AWS ElastiCache: ~$15-200/month depending on tier
- Self-hosted: Server costs only

### When You DON'T Need Redis

Skip Redis if:
- ✅ Single server deployment
- ✅ Small user base (< 1000 concurrent users)
- ✅ No load balancing planned
- ✅ Development/testing environment
- ✅ Budget constraints

**Remember**: The HTTP fallback provides reliability. Redis is ONLY for multi-server scaling, not messaging reliability.

## Future Enhancements

Potential improvements:
1. **Message Queue**: Queue failed messages for retry
2. **Optimistic UI**: Show message immediately with "sending" indicator
3. **Read Receipts**: Show when message was read
4. **Typing Indicators**: Show when other user is typing
5. **Message Editing**: Edit sent messages
6. **Message Reactions**: Add emoji reactions
7. **File Attachments**: Send images and files
8. **Message Search**: Search message history
9. **Push Notifications**: Browser push for new messages when tab inactive

## Conclusion

The hybrid messaging system provides the best of both worlds:
- ⚡ **Speed**: Real-time delivery via SignalR when connected
- 🛡️ **Reliability**: Automatic fallback ensures messages never fail
- 🔄 **Resilience**: Automatic reconnection with exponential backoff
- 📊 **Monitoring**: Visual connection status indicator
- 🎯 **UX**: Seamless experience regardless of connection state

Users get instant messaging when possible, with guaranteed delivery when network conditions are poor.

