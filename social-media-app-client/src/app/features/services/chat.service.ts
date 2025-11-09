import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environment';
import { firstValueFrom, Observable, Subject } from 'rxjs';
import { SignalRService } from '../../services/signalr.service';

export enum MessageType {
  Text = 0,
  File = 1
}

export interface MessageDto {
  id: string;
  chatId: string;
  senderId: string;
  senderUserName: string;
  senderDisplayName: string;
  content: string;
  messageType: MessageType;
  fileName?: string;
  fileSize?: number;
  mimeType?: string;
  isRead: boolean;
  createdAtUtc: Date;
}

export interface ChatDto {
  id: string;
  otherUserId: string;
  otherUserName: string;
  otherUserDisplayName: string;
  otherUserProfileImageUrl: string;
  lastMessageAtUtc: Date;
  lastMessageContent?: string;
  unreadCount: number;
}

export interface SendMessageRequest {
  receiverId: string;
  content: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly signalrService = inject(SignalRService);
  private readonly baseUrl = environment.apiUrl;
  private readonly messageSubject = new Subject<MessageDto>();
  private readonly readSubject = new Subject<string>();

  readonly messages$ = this.messageSubject.asObservable();
  readonly messagesRead$ = this.readSubject.asObservable();
  readonly connectionStatus$ = this.signalrService.getConnectionStatus$('chat');

  constructor() {
    this.initSignalR();
  }

  getUserChats(): Observable<ChatDto[]> {
    const url = `${this.baseUrl}/chats`;
    return this.http.get<ChatDto[]>(url);
  }

  getChatMessages(chatId: string, skip: number, take: number): Observable<MessageDto[]> {
    const url = `${this.baseUrl}/chats/${chatId}/messages?skip=${skip}&take=${take}`;
    return this.http.get<MessageDto[]>(url);
  }

  sendMessage(request: SendMessageRequest): Observable<MessageDto> {
    const url = `${this.baseUrl}/chats`;
    return this.http.post<MessageDto>(url, request);
  }

  uploadFile(file: File, receiverId: string): Observable<MessageDto> {
    const url = `${this.baseUrl}/chats/upload-file`;
    const formData = new FormData();
    formData.append('file', file);
    formData.append('receiverId', receiverId);
    return this.http.post<MessageDto>(url, formData);
  }

  markAsRead(chatId: string): Observable<void> {
    const url = `${this.baseUrl}/chats/${chatId}/read`;
    return this.http.post<void>(url, {});
  }

  getOrCreateChat(userId: string): Observable<{ chatId: string }> {
    const url = `${this.baseUrl}/chats/create/${userId}`;
    return this.http.post<{ chatId: string }>(url, {});
  }

  isConnected(): boolean {
    return this.signalrService.isConnected('chat');
  }

  getConnectionStatus(): string {
    return this.signalrService.getConnectionState('chat');
  }

  async sendMessageViaHub(receiverId: string, content: string): Promise<void> {
    if (!this.isConnected()) {
      throw new Error('SignalR not connected');
    }
    
    try {
      await this.signalrService.invoke('chat', 'SendMessage', receiverId, content);
    } catch (error) {
      console.error('Error sending message via hub', error);
      throw error;
    }
  }

  async sendMessageWithFallback(receiverId: string, content: string): Promise<MessageDto | void> {
    try {
      // Try SignalR first
      await this.sendMessageViaHub(receiverId, content);
      console.log('Message sent via SignalR');
    } catch (error) {
      // Fallback to HTTP API
      console.warn('SignalR failed, falling back to HTTP API', error);
      return await firstValueFrom(this.sendMessage({ receiverId, content }))
    }
  }

  async sendFileWithFallback(file: File, receiverId: string): Promise<MessageDto> {
    // Files are always sent via HTTP (more reliable for large data)
    return await firstValueFrom(this.uploadFile(file, receiverId));
  }

  markAsReadViaHub(chatId: string): void {
    if (this.isConnected()) {
      this.signalrService.invoke('chat', 'MarkAsRead', chatId)
        .catch(error => {
          console.error('Error marking as read via hub', error);
          // Fallback to HTTP
          this.markAsRead(chatId).subscribe();
        });
    } else {
      // Use HTTP if not connected
      this.markAsRead(chatId).subscribe();
    }
  }

  private initSignalR(): void {
    // Initialize SignalR connection with exponential backoff
    this.signalrService.createHubConnection('chat', {
      reconnectPolicy: {
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, max 60s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          if (retryContext.previousRetryCount === 3) return 30000;
          return 60000;
        }
      }
    });

    // Message handlers
    this.signalrService.on('chat', 'ReceiveMessage', (message: MessageDto) => {
      this.messageSubject.next(message);
    });

    this.signalrService.on('chat', 'MessagesMarkedAsRead', (chatId: string) => {
      this.readSubject.next(chatId);
    });
  }

  disconnect(): void {
    this.signalrService.disconnect('chat');
  }
}

