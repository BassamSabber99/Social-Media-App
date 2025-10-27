import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signalr.service';

export type CallState = 'idle' | 'initiating' | 'ringing' | 'connecting' | 'connected' | 'ended';

export interface IncomingCall {
  callerId: string;
  callerName?: string;
  callerUserName?: string;
}

export interface CallStateEvent {
  state: CallState;
  remoteUserId?: string;
  remoteUserName?: string;
}

@Injectable({ providedIn: 'root' })
export class WebRTCService {
  private readonly authService = inject(AuthService);
  private readonly signalrService = inject(SignalRService);
  
  private peerConnection: RTCPeerConnection | null = null;
  private localStream: MediaStream | null = null;
  private remoteStream: MediaStream | null = null;
  
  private currentRemoteUserId: string | null = null;
  private isInitiator = false;
  
  // ICE candidates queue for early candidates before remote description is set
  private iceCandidatesQueue: RTCIceCandidate[] = [];
  
  // Observables
  private readonly incomingCallSubject = new Subject<IncomingCall>();
  private readonly callStateSubject = new BehaviorSubject<CallStateEvent>({ state: 'idle' });
  private readonly localStreamSubject = new BehaviorSubject<MediaStream | null>(null);
  private readonly remoteStreamSubject = new BehaviorSubject<MediaStream | null>(null);
  
  readonly incomingCall$ = this.incomingCallSubject.asObservable();
  readonly callState$ = this.callStateSubject.asObservable();
  readonly localStream$ = this.localStreamSubject.asObservable();
  readonly remoteStream$ = this.remoteStreamSubject.asObservable();
  readonly hubConnectionStatus$ = this.signalrService.getConnectionStatus$('video');
  
  // Getters for current stream values
  getCurrentLocalStream(): MediaStream | null {
    return this.localStreamSubject.value;
  }
  
  getCurrentRemoteStream(): MediaStream | null {
    return this.remoteStreamSubject.value;
  }
  
  private readonly rtcConfig: RTCConfiguration = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' },
      { urls: 'stun:stun1.l.google.com:19302' }
    ]
  };
  
  constructor() {
    this.initSignalR();
  }
  // Add this new public method
public ensureInitialized(): void {
  if (!this.signalrService.isConnected('video')) {
    console.log('🔄 Initializing video hub...');
    this.signalrService.createHubConnection('video');
  }
}
  private async ensureHubConnected(): Promise<void> {
  // First, ensure hub is created
  this.ensureInitialized();
  
  // Then ensure it's connected
  await this.signalrService.ensureConnected('video');
  }
  
  async initiateCall(targetUserId: string, targetUserName?: string): Promise<void> {
    try {
      console.log('Initiating call to:', targetUserId);
      
      if (this.callStateSubject.value.state !== 'idle') {
        throw new Error('Call already in progress');
      }
      
      // Prevent calling yourself
      const currentUserId = this.authService.getUserId();
      if (targetUserId === currentUserId) {
        throw new Error('Cannot call yourself');
      }
      
      // Ensure hub is connected
      await this.ensureHubConnected();
      
      this.isInitiator = true;
      this.currentRemoteUserId = targetUserId;
      this.updateCallState('initiating', targetUserId, targetUserName);
      
      // Get local media
      console.log('Getting local media stream...');
      await this.startLocalStream();
      console.log('Local stream acquired:', this.localStream?.getTracks().map(t => t.kind));
      
      // Create peer connection
      console.log('Creating peer connection...');
      this.createPeerConnection();
      
      // Ensure all tracks are enabled before adding
      this.ensureTracksEnabled();
      
      // Add local stream tracks
      console.log('Adding local tracks to peer connection...');
      this.localStream?.getTracks().forEach(track => {
        console.log('🎤 Adding track:', {
          kind: track.kind,
          id: track.id,
          enabled: track.enabled,
          readyState: track.readyState,
          muted: track.muted
        });
        const sender = this.peerConnection!.addTrack(track, this.localStream!);
        console.log('✅ Track added to peer connection. Sender track:', sender.track?.id);
      });
      
      // Create and send offer
      console.log('Creating offer...');
      const offer = await this.peerConnection!.createOffer({
        offerToReceiveAudio: true,
        offerToReceiveVideo: true
      });
      
      // Optimize SDP for better audio quality
      offer.sdp = this.optimizeAudioQuality(offer.sdp!);
      
      await this.peerConnection!.setLocalDescription(offer);
      console.log('Local description set (offer)');
      
      // Send offer via SignalR
      console.log('Sending offer...');
      await this.signalrService.invoke('video', 'SendOffer', targetUserId, JSON.stringify(offer));
      console.log('Offer sent successfully');
      
      this.updateCallState('ringing', targetUserId, targetUserName);
    } catch (error) {
      console.error('Error initiating call:', error);
      this.endCall();
      throw error;
    }
  }
  
  async answerCall(callerId: string, callerName?: string): Promise<void> {
    try {
      console.log('Answering call from:', callerId);
      
      // Ensure hub is connected
      await this.ensureHubConnected();
      
      this.isInitiator = false;
      this.currentRemoteUserId = callerId;
      this.updateCallState('connecting', callerId, callerName);
      
      // Get local media
      console.log('Getting local media stream...');
      await this.startLocalStream();
      console.log('Local stream acquired:', this.localStream?.getTracks().map(t => t.kind));
      
      // Peer connection should already exist from receiving the offer
      if (!this.peerConnection) {
        throw new Error('No peer connection found');
      }
      
      // Ensure all tracks are enabled before adding
      this.ensureTracksEnabled();
      
      // Add local stream tracks to peer connection
      console.log('Adding local tracks to peer connection...');
      this.localStream?.getTracks().forEach(track => {
        console.log('🎤 Adding track:', {
          kind: track.kind,
          id: track.id,
          enabled: track.enabled,
          readyState: track.readyState,
          muted: track.muted
        });
        const sender = this.peerConnection!.addTrack(track, this.localStream!);
        console.log('✅ Track added to peer connection. Sender track:', sender.track?.id);
      });
      
      // Verify senders
      const senders = this.peerConnection!.getSenders();
      console.log('Total senders after adding tracks:', senders.length);
      senders.forEach((sender, index) => {
        console.log(`Sender ${index}:`, {
          kind: sender.track?.kind,
          id: sender.track?.id,
          enabled: sender.track?.enabled
        });
      });
      
      // Create and send answer
      console.log('Creating answer...');
      const answer = await this.peerConnection!.createAnswer();
      
      // Optimize SDP for better audio quality
      answer.sdp = this.optimizeAudioQuality(answer.sdp!);
      
      await this.peerConnection!.setLocalDescription(answer);
      console.log('Local description set (answer)');
      console.log('Answer SDP:', answer.sdp?.substring(0, 200) + '...');
      
      // Send answer via SignalR
      console.log('Sending answer to caller...');
      await this.signalrService.invoke('video', 'SendAnswer', callerId, JSON.stringify(answer));
      console.log('Answer sent successfully');
      
      // Process queued ICE candidates
      console.log('Processing queued ICE candidates...');
      await this.processQueuedIceCandidates();
      
      this.updateCallState('connected', callerId, callerName);
    } catch (error) {
      console.error('Error answering call:', error);
      this.endCall();
      throw error;
    }
  }
  
  declineCall(callerId: string): void {
    console.log('❌ Declining call from:', callerId);
    
    // Send decline signal to caller
    if (this.signalrService.isConnected('video')) {
      this.signalrService.invoke('video', 'HangupCall', callerId)
        .catch(error => console.error('Error sending decline signal:', error));
    }
    
    // Clean up peer connection
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    
    // Clear remote stream (no local stream to stop since we never answered)
    this.remoteStream = null;
    this.remoteStreamSubject.next(null);
    
    // Reset state - go directly to idle (not 'ended')
    this.currentRemoteUserId = null;
    this.isInitiator = false;
    this.iceCandidatesQueue = [];
    this.updateCallState('idle');
    
    console.log('✅ Call declined and state reset to idle');
  }
  
  endCall(): void {
    // Send hangup signal to remote user if in a call
    if (this.currentRemoteUserId && this.signalrService.isConnected('video')) {
      this.signalrService.invoke('video', 'HangupCall', this.currentRemoteUserId)
        .catch(error => console.error('Error sending hangup signal:', error));
    }
    
    // Close peer connection
    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }
    
    // Stop local stream
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = null;
      this.localStreamSubject.next(null);
    }
    
    // Clear remote stream
    this.remoteStream = null;
    this.remoteStreamSubject.next(null);
    
    // Reset state
    this.currentRemoteUserId = null;
    this.isInitiator = false;
    this.iceCandidatesQueue = [];
    this.updateCallState('ended');
    
    // After a brief delay, reset to idle
    setTimeout(() => {
      if (this.callStateSubject.value.state === 'ended') {
        this.updateCallState('idle');
      }
    }, 1000);
  }
  
  toggleAudio(): boolean {
    if (!this.localStream) return false;
    
    const audioTrack = this.localStream.getAudioTracks()[0];
    if (audioTrack) {
      audioTrack.enabled = !audioTrack.enabled;
      return audioTrack.enabled;
    }
    return false;
  }
  
  toggleVideo(): boolean {
    if (!this.localStream) return false;
    
    const videoTrack = this.localStream.getVideoTracks()[0];
    if (videoTrack) {
      videoTrack.enabled = !videoTrack.enabled;
      console.log('📹 Video toggled:', videoTrack.enabled ? 'ON' : 'OFF');
      
      // Re-emit stream to notify all subscribers
      this.localStreamSubject.next(this.localStream);
      
      return videoTrack.enabled;
    }
    return false;
  }
  
  private ensureTracksEnabled(): void {
    if (!this.localStream) return;
    
    const audioTrack = this.localStream.getAudioTracks()[0];
    const videoTrack = this.localStream.getVideoTracks()[0];
    
    if (audioTrack && !audioTrack.enabled) {
      audioTrack.enabled = true;
      console.log('✅ Audio track enabled (was disabled)');
    }
    if (videoTrack && !videoTrack.enabled) {
      videoTrack.enabled = true;
      console.log('✅ Video track enabled (was disabled)');
    }
    
    console.log('📊 Track states:', {
      audio: audioTrack ? { enabled: audioTrack.enabled, readyState: audioTrack.readyState } : 'none',
      video: videoTrack ? { enabled: videoTrack.enabled, readyState: videoTrack.readyState } : 'none'
    });
  }
  
  private optimizeAudioQuality(sdp: string): string {
    // Set audio bitrate to prioritize quality
    sdp = sdp.replace(/(m=audio.*\r\n)/g, (match) => {
      return match + 'b=AS:64\r\n'; // 64 kbps for audio
    });
    
    // Set video bitrate to prevent it from hogging bandwidth
    sdp = sdp.replace(/(m=video.*\r\n)/g, (match) => {
      return match + 'b=AS:512\r\n'; // 512 kbps for video (lower than default)
    });
    
    // Prefer Opus codec for audio (best quality)
    sdp = sdp.replace(/(m=audio.*)(RTP\/SAVPF\s)(\d+)/g, (match, prefix, proto, payload) => {
      return match; // Opus is usually first by default
    });
    
    console.log('✅ SDP optimized for audio quality');
    return sdp;
  }
  
  isAudioEnabled(): boolean {
    return this.localStream?.getAudioTracks()[0]?.enabled ?? false;
  }
  
  isVideoEnabled(): boolean {
    return this.localStream?.getVideoTracks()[0]?.enabled ?? false;
  }
  
  private async startLocalStream(): Promise<void> {
    try {
      // Check if running in secure context
      if (!window.isSecureContext) {
        const hostname = window.location.hostname;
        if (hostname !== 'localhost' && hostname !== '127.0.0.1') {
          throw new Error(
            'Video calling requires HTTPS or localhost. ' +
            'Current URL: ' + window.location.origin + '. ' +
            'Please use https:// or access via localhost/127.0.0.1'
          );
        }
      }

      // Check if mediaDevices is supported
      if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error(
          'Your browser does not support video calling or is in an insecure context. ' +
          'Please use HTTPS or localhost.'
        );
      }

      this.localStream = await navigator.mediaDevices.getUserMedia({
        video: { 
          width: { ideal: 640, max: 1280 }, 
          height: { ideal: 480, max: 720 },
          frameRate: { ideal: 24, max: 30 }
        },
        audio: {
          echoCancellation: { ideal: true },
          noiseSuppression: { ideal: true },
          autoGainControl: { ideal: true },
          sampleRate: { ideal: 48000 },
          channelCount: { ideal: 1 }
        }
      });
      this.localStreamSubject.next(this.localStream);
    } catch (error: any) {
      console.error('Error accessing media devices:', error);
      
      let errorMessage = 'Failed to access camera/microphone';
      
      if (error.name === 'NotAllowedError' || error.name === 'PermissionDeniedError') {
        errorMessage = 'Camera/microphone permission denied. Please allow access in your browser settings.';
      } else if (error.name === 'NotFoundError' || error.name === 'DevicesNotFoundError') {
        errorMessage = 'No camera/microphone found. Please connect a device and try again.';
      } else if (error.name === 'NotReadableError' || error.name === 'TrackStartError') {
        errorMessage = 'Camera/microphone is already in use by another application.';
      } else if (error.name === 'OverconstrainedError') {
        errorMessage = 'Camera does not meet the required specifications.';
      } else if (error.name === 'SecurityError') {
        errorMessage = 'Video calling requires HTTPS. Please use https:// or access via localhost.';
      } else if (error.message && error.message.includes('secure')) {
        errorMessage = error.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      throw new Error(errorMessage);
    }
  }
  
  private createPeerConnection(): void {
    this.peerConnection = new RTCPeerConnection(this.rtcConfig);
    
    console.log('Peer connection created');
    
    // Handle ICE candidates
    this.peerConnection.onicecandidate = (event) => {
      if (event.candidate && this.currentRemoteUserId) {
        console.log('Sending ICE candidate:', event.candidate.type);
        this.signalrService.invoke('video', 'SendCandidate', this.currentRemoteUserId, JSON.stringify(event.candidate))
          .catch(err => console.error('Error sending ICE candidate:', err));
      }
    };
    
    // Handle remote stream - CRITICAL for receiving video from other user
    this.peerConnection.ontrack = (event) => {
      console.log('🎥 ontrack event fired!', {
        kind: event.track.kind,
        id: event.track.id,
        readyState: event.track.readyState,
        streams: event.streams?.length || 0
      });
      
      // Use the stream from the event if available (most reliable method)
      if (event.streams && event.streams.length > 0) {
        console.log('✅ Using stream from event.streams[0]');
        this.remoteStream = event.streams[0];
        this.remoteStreamSubject.next(this.remoteStream);
        
        // Log all tracks in the stream
        console.log('Remote stream tracks:', this.remoteStream.getTracks().map(t => ({
          kind: t.kind,
          id: t.id,
          enabled: t.enabled,
          readyState: t.readyState
        })));
      } else {
        // Fallback: manually create and manage the stream
        console.log('⚠️ event.streams not available, creating MediaStream manually');
        if (!this.remoteStream) {
          this.remoteStream = new MediaStream();
          console.log('Created new MediaStream');
        }
        this.remoteStream.addTrack(event.track);
        console.log('Added track to stream. Total tracks:', this.remoteStream.getTracks().length);
        // Always emit to ensure video element gets updated
        this.remoteStreamSubject.next(this.remoteStream);
      }
    };
    
    // Handle ICE connection state
    this.peerConnection.oniceconnectionstatechange = () => {
      console.log('ICE connection state:', this.peerConnection?.iceConnectionState);
    };
    
    // Handle connection state changes
    this.peerConnection.onconnectionstatechange = () => {
      console.log('Peer connection state:', this.peerConnection?.connectionState);
      
      // Only end call on disconnect/fail - don't auto-set to connected
      // Let the answer/offer flow control the state
      if (this.peerConnection?.connectionState === 'disconnected' || 
          this.peerConnection?.connectionState === 'failed') {
        console.log('⚠️ Peer connection lost, ending call');
        this.endCall();
      }
    };
    
    // Handle signaling state changes
    this.peerConnection.onsignalingstatechange = () => {
      console.log('Signaling state:', this.peerConnection?.signalingState);
    };
  }
  
  private async processQueuedIceCandidates(): Promise<void> {
    for (const candidate of this.iceCandidatesQueue) {
      try {
        await this.peerConnection?.addIceCandidate(candidate);
      } catch (error) {
        console.error('Error adding queued ICE candidate:', error);
      }
    }
    this.iceCandidatesQueue = [];
  }
  
  private updateCallState(state: CallState, remoteUserId?: string, remoteUserName?: string): void {
    this.callStateSubject.next({ state, remoteUserId, remoteUserName });
  }
  
  private initSignalR(): void {
    // Initialize SignalR connection
    this.signalrService.createHubConnection('video');
    
    // Handle incoming offer
    this.signalrService.on('video', 'ReceiveOffer', async (callerId: string, callerName: string, sdp: string) => {
      console.log('📞 Received offer from:', callerId, callerName);
      try {
        // Create peer connection if not exists
        if (!this.peerConnection) {
          console.log('Creating peer connection for incoming offer...');
          this.createPeerConnection();
        }
        
        const offer: RTCSessionDescriptionInit = JSON.parse(sdp);
        console.log('Offer SDP received:', offer.sdp?.substring(0, 200) + '...');
        
        await this.peerConnection!.setRemoteDescription(new RTCSessionDescription(offer));
        console.log('✅ Remote description set (offer)');
        
        // Check what transceivers were created
        const transceivers = this.peerConnection!.getTransceivers();
        console.log('Transceivers after setting offer:', transceivers.length);
        transceivers.forEach((transceiver, index) => {
          console.log(`Transceiver ${index}:`, {
            direction: transceiver.direction,
            mid: transceiver.mid,
            receiver_kind: transceiver.receiver?.track?.kind
          });
        });
        
        // Notify UI about incoming call
        this.incomingCallSubject.next({ 
          callerId, 
          callerName,
          callerUserName: callerName 
        });
      } catch (error) {
        console.error('Error handling offer:', error);
      }
    });
    
    // Handle incoming answer
    this.signalrService.on('video', 'ReceiveAnswer', async (callerId: string, callerName: string, sdp: string) => {
      console.log('📞 Received answer from:', callerId);
      try {
        const answer: RTCSessionDescriptionInit = JSON.parse(sdp);
        console.log('Answer SDP received:', answer.sdp?.substring(0, 200) + '...');
        
        await this.peerConnection?.setRemoteDescription(new RTCSessionDescription(answer));
        console.log('✅ Remote description set (answer)');
        
        // Check receivers
        const receivers = this.peerConnection?.getReceivers() || [];
        console.log('Total receivers after answer:', receivers.length);
        receivers.forEach((receiver, index) => {
          console.log(`Receiver ${index}:`, {
            kind: receiver.track?.kind,
            id: receiver.track?.id,
            enabled: receiver.track?.enabled,
            readyState: receiver.track?.readyState
          });
        });
        
        // Process queued ICE candidates
        await this.processQueuedIceCandidates();
        
        this.updateCallState('connected', callerId, callerName);
      } catch (error) {
        console.error('Error handling answer:', error);
      }
    });
    
    // Handle incoming ICE candidate
    this.signalrService.on('video', 'ReceiveCandidate', async (senderId: string, candidateJson: string) => {
      console.log('Received ICE candidate from:', senderId);
      try {
        const candidate: RTCIceCandidateInit = JSON.parse(candidateJson);
        
        if (this.peerConnection?.remoteDescription) {
          await this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
        } else {
          // Queue candidate if remote description not set yet
          this.iceCandidatesQueue.push(new RTCIceCandidate(candidate));
        }
      } catch (error) {
        console.error('Error handling ICE candidate:', error);
      }
    });
    
    // Handle call hangup
    this.signalrService.on('video', 'CallHangup', (callerId: string) => {
      console.log('Call hangup from:', callerId);
      // End the call without sending hangup signal back (to avoid loop)
      const remoteUserId = this.currentRemoteUserId;
      this.currentRemoteUserId = null; // Clear before ending to prevent sending hangup back
      
      // Close peer connection
      if (this.peerConnection) {
        this.peerConnection.close();
        this.peerConnection = null;
      }
      
      // Stop local stream
      if (this.localStream) {
        this.localStream.getTracks().forEach(track => track.stop());
        this.localStream = null;
        this.localStreamSubject.next(null);
      }
      
      // Clear remote stream
      this.remoteStream = null;
      this.remoteStreamSubject.next(null);
      
      // Reset state
      this.isInitiator = false;
      this.iceCandidatesQueue = [];
      this.updateCallState('ended');
      
      // After a brief delay, reset to idle
      setTimeout(() => {
        if (this.callStateSubject.value.state === 'ended') {
          this.updateCallState('idle');
        }
      }, 1000);
    });
  }
  
  disconnect(): void {
    this.endCall();
    this.signalrService.disconnect('video');
  }
}

