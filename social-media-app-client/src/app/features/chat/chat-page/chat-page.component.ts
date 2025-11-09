import { Component, OnInit, OnDestroy, signal, computed, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService, ChatDto, MessageDto, MessageType } from '../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { WebRTCService } from '../../services/webrtc.service';
import { VoiceRecorderService } from '../../services/voice-recorder.service';
import { Subscription } from 'rxjs';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { ScrollPanelModule } from 'primeng/scrollpanel';
import { BadgeModule } from 'primeng/badge';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { NavbarComponent } from '../../../shared/navbar/navbar.component';
import { LucideAngularModule, Send, MessageCircle, ArrowLeft, Video, Paperclip, Mic, X, Download, FileIcon, Music } from 'lucide-angular';

@Component({
  selector: 'app-chat-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    AvatarModule,
    ScrollPanelModule,
    BadgeModule,
    TooltipModule,
    ToastModule,
    NavbarComponent,
    LucideAngularModule
  ],
  providers: [MessageService],
  templateUrl: './chat-page.component.html',
  styleUrl: './chat-page.component.scss'
})
export class ChatPageComponent implements OnInit, OnDestroy {
  readonly SendIcon = Send;
  readonly MessageCircleIcon = MessageCircle;
  readonly ArrowLeftIcon = ArrowLeft;
  readonly VideoIcon = Video;
  readonly PaperclipIcon = Paperclip;
  readonly MicIcon = Mic;
  readonly XIcon = X;
  readonly DownloadIcon = Download;
  readonly FileIcon = FileIcon;
  readonly MusicIcon = Music;
  readonly MessageType = MessageType;

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  chats = signal<ChatDto[]>([]);
  selectedChat = signal<ChatDto | null>(null);
  messages = signal<MessageDto[]>([]);
  messageContent = signal('');
  loading = signal(false);
  sending = signal(false);
  isConnected = signal(true);
  showMobileChatList = signal(true);
  isRecording = signal(false);
  recordingDuration = signal(0);
  uploadingFile = signal(false);
  currentUserId: string | null = null;

  private subscriptions: Subscription[] = [];

  readonly sortedChats = computed(() => {
    const chatList = [...this.chats()];
    return chatList.sort((a, b) => 
      new Date(b.lastMessageAtUtc).getTime() - new Date(a.lastMessageAtUtc).getTime()
    );
  });

  constructor(
    private readonly chatService: ChatService,
    private readonly authService: AuthService,
    private readonly webrtcService: WebRTCService,
    private readonly voiceRecorderService: VoiceRecorderService,
    private readonly messageService: MessageService
  ) {
    this.currentUserId = this.authService.getUserId();
  }

  ngOnInit(): void {
    this.loadChats();
    this.setupRealtimeListeners();
    this.setupConnectionStatusListener();
    this.setupVoiceRecorderListener();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    if (this.isRecording()) {
      this.voiceRecorderService.cancelRecording();
    }
  }

  private setupRealtimeListeners(): void {
    this.subscriptions.push(
      this.chatService.messages$.subscribe(message => {
        const currentChatId = this.selectedChat()?.id;
        
        // Update messages if this message belongs to the selected chat
        if (currentChatId === message.chatId) {
          this.messages.update(msgs => [...msgs, message]);
          
          // Mark as read if we're viewing the chat
          if (message.senderId !== this.currentUserId) {
            this.chatService.markAsReadViaHub(message.chatId);
          }
          
          setTimeout(() => this.scrollToBottom(), 100);
        }
        
        // Update chat list
        this.updateChatInList(message);
      })
    );

    this.subscriptions.push(
      this.chatService.messagesRead$.subscribe(chatId => {
        // Update unread count in chat list
        this.chats.update(chats => 
          chats.map(chat => 
            chat.id === chatId ? { ...chat, unreadCount: 0 } : chat
          )
        );

        // Mark messages as read in current conversation
        if (this.selectedChat()?.id === chatId) {
          this.messages.update(msgs =>
            msgs.map(msg => ({ ...msg, isRead: true }))
          );
        }
      })
    );
  }

  private updateChatInList(message: MessageDto): void {
    this.chats.update(chats => {
      const existingChatIndex = chats.findIndex(c => c.id === message.chatId);
      
      if (existingChatIndex >= 0) {
        const updatedChats = [...chats];
        const chat = updatedChats[existingChatIndex];
        
        updatedChats[existingChatIndex] = {
          ...chat,
          lastMessageContent: message.content,
          lastMessageAtUtc: message.createdAtUtc,
          unreadCount: message.senderId === this.currentUserId || this.selectedChat()?.id === message.chatId
            ? chat.unreadCount
            : chat.unreadCount + 1
        };
        
        return updatedChats;
      } else {
        // Reload chats if a new chat was created
        this.loadChats();
        return chats;
      }
    });
  }

  loadChats(): void {
    this.loading.set(true);
    this.chatService.getUserChats().subscribe({
      next: chats => {
        this.chats.set(chats);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error loading chats:', err);
        this.loading.set(false);
      }
    });
  }

  selectChat(chat: ChatDto): void {
    this.selectedChat.set(chat);
    this.messages.set([]);
    this.showMobileChatList.set(false);
    this.loadMessages(chat.id);
    
    if (chat.unreadCount > 0) {
      this.chatService.markAsRead(chat.id).subscribe({
        next: () => {
          this.chats.update(chats =>
            chats.map(c => c.id === chat.id ? { ...c, unreadCount: 0 } : c)
          );
        }
      });
    }
  }

  backToMobileChatList(): void {
    this.showMobileChatList.set(true);
    this.selectedChat.set(null);
  }

  private loadMessages(chatId: string): void {
    this.chatService.getChatMessages(chatId, 0, 50).subscribe({
      next: messages => {
        this.messages.set(messages);
        setTimeout(() => this.scrollToBottom(), 100);
      },
      error: err => console.error('Error loading messages:', err)
    });
  }

  async sendMessage(): Promise<void> {
    const content = this.messageContent().trim();
    const chat = this.selectedChat();
    
    if (!content || !chat || this.sending()) return;

    this.messageContent.set('');
    this.sending.set(true);
    
    try {
      const result = await this.chatService.sendMessageWithFallback(chat.otherUserId, content);
      
      // If HTTP fallback was used, manually add message to UI
      if (result) {
        this.messages.update(msgs => [...msgs, result]);
        setTimeout(() => this.scrollToBottom(), 100);
      }
    } catch (error) {
      console.error('Failed to send message:', error);
      // Restore message content on error
      this.messageContent.set(content);
      // Could show error toast here
    } finally {
      this.sending.set(false);
    }
  }

  private setupConnectionStatusListener(): void {
    this.subscriptions.push(
      this.chatService.connectionStatus$.subscribe(connected => {
        this.isConnected.set(connected);
        if (connected) {
          console.log('Chat reconnected - messages will be sent in real-time');
        } else {
          console.log('Chat disconnected - will use HTTP fallback');
        }
      })
    );
  }

  private scrollToBottom(): void {
    const element = document.querySelector('.messages-container');
    if (element) {
      element.scrollTop = element.scrollHeight;
    }
  }

  formatTime(date: Date): string {
    const messageDate = new Date(date);
    const now = new Date();
    const diff = now.getTime() - messageDate.getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    
    if (hours < 24) {
      return messageDate.toLocaleTimeString('en-US', { 
        hour: 'numeric', 
        minute: '2-digit',
        hour12: true 
      });
    } else if (hours < 48) {
      return 'Yesterday';
    } else {
      return messageDate.toLocaleDateString('en-US', { 
        month: 'short', 
        day: 'numeric' 
      });
    }
  }

  formatChatTime(date: Date): string {
    return this.formatTime(date);
  }

  getAvatarInitials(displayName: string): string {
    return displayName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  getUsername(username: string): string {
    return '@' + username;
  }

  canCallCurrentChat(): boolean {
    const chat = this.selectedChat();
    return !!chat && chat.otherUserId !== this.currentUserId;
  }

  initiateVideoCall(): void {
    const chat = this.selectedChat();
    if (!chat) return;
    
    // Prevent calling yourself
    if (chat.otherUserId === this.currentUserId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Cannot Call',
        detail: 'You cannot call yourself',
        life: 3000
      });
      return;
    }
    
    this.webrtcService.initiateCall(chat.otherUserId, chat.otherUserDisplayName)
      .catch(error => {
        console.error('Failed to initiate video call:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Call Failed',
          detail: error.message || 'Failed to initiate video call',
          life: 5000
        });
      });
  }

  selectFile(): void {
    this.fileInput.nativeElement.click();
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    
    if (!file) return;

    const chat = this.selectedChat();
    if (!chat) return;

    // Max file size check (10 MB for files, 5 MB for voice)
    const maxSize = file.type.startsWith('audio/') ? 5 * 1024 * 1024 : 10 * 1024 * 1024;
    if (file.size > maxSize) {
      this.messageService.add({
        severity: 'error',
        summary: 'File Too Large',
        detail: `Maximum file size is ${maxSize / 1024 / 1024} MB`,
        life: 5000
      });
      input.value = '';
      return;
    }

    this.uploadingFile.set(true);

    try {
      const message = await this.chatService.sendFileWithFallback(file, chat.otherUserId);
      this.messages.update(msgs => [...msgs, message]);
      setTimeout(() => this.scrollToBottom(), 100);
      
      this.messageService.add({
        severity: 'success',
        summary: 'File Sent',
        detail: 'File uploaded successfully',
        life: 3000
      });
    } catch (error: any) {
      console.error('Failed to upload file:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Upload Failed',
        detail: error.error?.error || 'Failed to upload file',
        life: 5000
      });
    } finally {
      this.uploadingFile.set(false);
      input.value = '';
    }
  }

  async startVoiceRecording(): Promise<void> {
    try {
      await this.voiceRecorderService.startRecording();
      this.isRecording.set(true);
    } catch (error: any) {
      console.error('Failed to start recording:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Recording Failed',
        detail: error.message || 'Failed to start recording',
        life: 5000
      });
    }
  }

  stopVoiceRecording(): void {
    this.voiceRecorderService.stopRecording();
  }

  cancelVoiceRecording(): void {
    this.voiceRecorderService.cancelRecording();
    this.isRecording.set(false);
    this.recordingDuration.set(0);
  }

  private setupVoiceRecorderListener(): void {
    this.subscriptions.push(
      this.voiceRecorderService.recordingState$.subscribe(async state => {
        this.isRecording.set(state.isRecording);
        this.recordingDuration.set(state.duration);

        // When recording stops and we have audio blob, send it
        if (!state.isRecording && state.audioBlob) {
          const chat = this.selectedChat();
          if (!chat) return;

          this.uploadingFile.set(true);

          try {
            const mimeType = state.audioBlob.type;
            const extension = this.voiceRecorderService.getFileExtension(mimeType);
            const fileName = `voice-note-${Date.now()}.${extension}`;
            const file = new File([state.audioBlob], fileName, { type: mimeType });

            const message = await this.chatService.sendFileWithFallback(file, chat.otherUserId);
            this.messages.update(msgs => [...msgs, message]);
            setTimeout(() => this.scrollToBottom(), 100);

            this.messageService.add({
              severity: 'success',
              summary: 'Voice Note Sent',
              detail: 'Voice note sent successfully',
              life: 3000
            });
          } catch (error: any) {
            console.error('Failed to send voice note:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Send Failed',
              detail: error.error?.error || 'Failed to send voice note',
              life: 5000
            });
          } finally {
            this.uploadingFile.set(false);
          }
        }
      })
    );
  }

  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  isVoiceNote(message: MessageDto): boolean {
    return message.messageType === MessageType.File && 
           !!message.mimeType?.startsWith('audio/');
  }

  isFileMessage(message: MessageDto): boolean {
    return message.messageType === MessageType.File && 
           !message.mimeType?.startsWith('audio/');
  }

  getFileIcon(mimeType?: string): any {
    if (!mimeType) return this.FileIcon;
    if (mimeType.startsWith('audio/')) return this.MusicIcon;
    return this.FileIcon;
  }
}

