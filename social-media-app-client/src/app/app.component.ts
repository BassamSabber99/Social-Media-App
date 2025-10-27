import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { VideoCallComponent } from './features/video-call/video-call.component';
import { IncomingCallModalComponent } from './features/video-call/incoming-call-modal/incoming-call-modal.component';
import { WebRTCService } from './features/services/webrtc.service';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, VideoCallComponent, IncomingCallModalComponent, ToastModule],
  providers: [MessageService],
  template: `
    <router-outlet></router-outlet>
    
    @if (isInCall()) {
      <app-video-call></app-video-call>
    }
    
    <app-incoming-call-modal></app-incoming-call-modal>

    <p-toast></p-toast>
  `,
  styles: [':host { display: block; min-height: 100vh; background-color: var(--surface-ground, #f4f4f4); }']
})
export class AppComponent implements OnInit {
  isInCall = signal(false);
  
  constructor(private webrtcService: WebRTCService) {}
  
  ngOnInit(): void {
    this.webrtcService.callState$.subscribe(event => {
      this.isInCall.set(event.state !== 'idle' && event.state !== 'ended');
    });
  }
}
