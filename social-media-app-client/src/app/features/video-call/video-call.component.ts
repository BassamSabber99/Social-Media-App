import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { WebRTCService, CallState } from '../services/webrtc.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-video-call',
  standalone: true,
  imports: [CommonModule, ButtonModule, TooltipModule],
  templateUrl: './video-call.component.html',
  styleUrls: ['./video-call.component.scss']
})
export class VideoCallComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('localVideo') localVideo!: ElementRef<HTMLVideoElement>;
  @ViewChild('remoteVideo') remoteVideo!: ElementRef<HTMLVideoElement>;
  
  private readonly webrtcService = inject(WebRTCService);
  private readonly destroy$ = new Subject<void>();
  
  callState: CallState = 'idle';
  remoteUserName = 'Unknown User';
  isAudioEnabled = true;
  isVideoEnabled = true;
  callDuration = 0;
  
  private callTimer: any;
  
  ngOnInit(): void {
    // Subscribe to call state
    this.webrtcService.callState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        this.callState = event.state;
        this.remoteUserName = event.remoteUserName || 'Unknown User';
        
        if (event.state === 'connected') {
          this.startCallTimer();
        } else {
          this.stopCallTimer();
        }
      });
    
    // Subscribe to local stream
    this.webrtcService.localStream$
      .pipe(takeUntil(this.destroy$))
      .subscribe(stream => {
        console.log('📹 Local stream updated:', stream?.getTracks().map(t => t.kind));
        if (stream && this.localVideo) {
          this.localVideo.nativeElement.srcObject = stream;
          // CRITICAL: Always mute local video to prevent hearing yourself
          this.localVideo.nativeElement.muted = true;
          this.localVideo.nativeElement.volume = 0;
          console.log('✅ Local video element updated and MUTED');
          // Ensure video plays
          this.localVideo.nativeElement.play().catch(e => {
            console.log('Autoplay prevented:', e);
          });
        } else if (stream) {
          console.log('⚠️ Local stream available but video element not ready - retrying...');
          // Retry after Angular renders the new element (when *ngIf creates it)
          setTimeout(() => {
            if (stream && this.localVideo) {
              console.log('🔄 Retrying local video assignment...');
              this.localVideo.nativeElement.srcObject = stream;
              this.localVideo.nativeElement.muted = true;
              this.localVideo.nativeElement.volume = 0;
              console.log('✅ Local video MUTED on retry');
              this.localVideo.nativeElement.play().catch(e => {
                console.log('Autoplay prevented:', e);
              });
            }
          }, 100);
        }
      });
    
    // Subscribe to remote stream
    this.webrtcService.remoteStream$
      .pipe(takeUntil(this.destroy$))
      .subscribe(stream => {
        console.log('🎥 Remote stream updated:', stream?.getTracks().map(t => t.kind));
        if (stream && this.remoteVideo) {
          this.remoteVideo.nativeElement.srcObject = stream;
          // IMPORTANT: Remote video should NOT be muted (you want to hear the other person)
          this.remoteVideo.nativeElement.muted = false;
          this.remoteVideo.nativeElement.volume = 1;
          console.log('✅ Remote video element updated and UNMUTED');
          
          // Force play (some browsers need this)
          setTimeout(() => {
            this.remoteVideo.nativeElement.play().catch(e => {
              console.log('Autoplay prevented, user interaction needed:', e);
            });
          }, 100);
        } else if (stream) {
          console.log('⚠️ Remote stream available but video element not ready - will retry when view is ready');
          // Retry when view is initialized
          setTimeout(() => {
            if (stream && this.remoteVideo) {
              console.log('🔄 Retrying remote video element assignment...');
              this.remoteVideo.nativeElement.srcObject = stream;
              this.remoteVideo.nativeElement.muted = false;
              this.remoteVideo.nativeElement.volume = 1;
              console.log('✅ Remote video element updated and UNMUTED (retry)');
              this.remoteVideo.nativeElement.play().catch(e => {
                console.log('Autoplay prevented:', e);
              });
            }
          }, 500);
        } else {
          console.log('❌ Remote stream is null/undefined');
        }
      });
  }
  
  ngAfterViewInit(): void {
    // Immediately apply any existing streams when view is ready
    setTimeout(() => {
      const localStream = this.webrtcService.getCurrentLocalStream();
      const remoteStream = this.webrtcService.getCurrentRemoteStream();
      
      if (localStream && this.localVideo) {
        console.log('🔄 Applying existing local stream to video element');
        this.localVideo.nativeElement.srcObject = localStream;
        this.localVideo.nativeElement.muted = true;
        this.localVideo.nativeElement.volume = 0;
        console.log('✅ Local video MUTED in ngAfterViewInit');
      }
      
      if (remoteStream && this.remoteVideo) {
        console.log('🔄 Applying existing remote stream to video element');
        this.remoteVideo.nativeElement.srcObject = remoteStream;
        this.remoteVideo.nativeElement.muted = false;
        this.remoteVideo.nativeElement.volume = 1;
        console.log('✅ Remote video UNMUTED in ngAfterViewInit');
        this.remoteVideo.nativeElement.play().catch(e => {
          console.log('Autoplay prevented:', e);
        });
      }
    }, 100);
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopCallTimer();
  }
  
  hangUp(): void {
    this.webrtcService.endCall();
  }
  
  toggleAudio(): void {
    this.isAudioEnabled = this.webrtcService.toggleAudio();
  }
  
  toggleVideo(): void {
    this.isVideoEnabled = this.webrtcService.toggleVideo();
    // No need to force refresh - the stream subscription will handle it
    // when the video element is recreated by *ngIf
  }
  
  get callStatusText(): string {
    switch (this.callState) {
      case 'initiating':
        return 'Initiating call...';
      case 'ringing':
        return 'Ringing...';
      case 'connecting':
        return 'Connecting...';
      case 'connected':
        return this.formatDuration(this.callDuration);
      case 'ended':
        return 'Call ended';
      default:
        return '';
    }
  }
  
  private startCallTimer(): void {
    this.callDuration = 0;
    this.callTimer = setInterval(() => {
      this.callDuration++;
    }, 1000);
  }
  
  private stopCallTimer(): void {
    if (this.callTimer) {
      clearInterval(this.callTimer);
      this.callTimer = null;
    }
    this.callDuration = 0;
  }
  
  private formatDuration(seconds: number): string {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    
    if (hours > 0) {
      return `${hours}:${this.pad(minutes)}:${this.pad(secs)}`;
    }
    return `${minutes}:${this.pad(secs)}`;
  }
  
  private pad(num: number): string {
    return num.toString().padStart(2, '0');
  }
}

