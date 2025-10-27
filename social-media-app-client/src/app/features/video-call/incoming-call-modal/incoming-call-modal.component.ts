import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { WebRTCService, IncomingCall } from '../../services/webrtc.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-incoming-call-modal',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule],
  templateUrl: './incoming-call-modal.component.html',
  styleUrls: ['./incoming-call-modal.component.scss']
})
export class IncomingCallModalComponent implements OnInit, OnDestroy {
  private readonly webrtcService = inject(WebRTCService);
  private readonly destroy$ = new Subject<void>();
  
  incomingCall: IncomingCall | null = null;
  showModal = false;
  
  private autoDeclineTimeout: any;
  private readonly AUTO_DECLINE_DELAY = 30000; // 30 seconds
  
  ngOnInit(): void {
    console.log('📱 Incoming call modal component initialized');
    
    this.webrtcService.incomingCall$
      .pipe(takeUntil(this.destroy$))
      .subscribe(call => {
        console.log('📞 Incoming call received in modal:', call);
        this.incomingCall = call;
        this.showModal = true;
        console.log('✅ Modal visibility set to true. showModal =', this.showModal);
        this.startAutoDeclineTimer();
      });
    
    // Hide modal when call state changes
    this.webrtcService.callState$
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        console.log('📞 Call state in modal:', event.state);
        if (event.state !== 'idle' && event.state !== 'ringing') {
          console.log('⚠️ Hiding modal due to state:', event.state);
          this.hideModal();
        }
      });
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearAutoDeclineTimer();
  }
  
  async acceptCall(): Promise<void> {
    if (this.incomingCall) {
      try {
        await this.webrtcService.answerCall(
          this.incomingCall.callerId,
          this.incomingCall.callerName
        );
        this.hideModal();
      } catch (error) {
        console.error('Error accepting call:', error);
        this.hideModal();
      }
    }
  }
  
  declineCall(): void {
    if (this.incomingCall) {
      this.webrtcService.declineCall(this.incomingCall.callerId);
      this.hideModal();
    }
  }
  
  private hideModal(): void {
    this.showModal = false;
    this.incomingCall = null;
    this.clearAutoDeclineTimer();
  }
  
  private startAutoDeclineTimer(): void {
    this.clearAutoDeclineTimer();
    this.autoDeclineTimeout = setTimeout(() => {
      this.declineCall();
    }, this.AUTO_DECLINE_DELAY);
  }
  
  private clearAutoDeclineTimer(): void {
    if (this.autoDeclineTimeout) {
      clearTimeout(this.autoDeclineTimeout);
      this.autoDeclineTimeout = null;
    }
  }
}

