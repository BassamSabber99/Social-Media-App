import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface RecordingState {
  isRecording: boolean;
  duration: number; // in seconds
  audioBlob: Blob | null;
}

@Injectable({ providedIn: 'root' })
export class VoiceRecorderService {
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];
  private stream: MediaStream | null = null;
  private startTime: number = 0;
  private timerInterval: any = null;

  private recordingStateSubject = new BehaviorSubject<RecordingState>({
    isRecording: false,
    duration: 0,
    audioBlob: null
  });

  readonly recordingState$ = this.recordingStateSubject.asObservable();

  async startRecording(): Promise<void> {
    try {
      // Request microphone access
      this.stream = await navigator.mediaDevices.getUserMedia({ 
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        } 
      });

      // Determine the best supported MIME type
      const mimeType = this.getSupportedMimeType();
      
      this.mediaRecorder = new MediaRecorder(this.stream, {
        mimeType: mimeType
      });

      this.audioChunks = [];
      this.startTime = Date.now();

      // Update duration every second
      this.timerInterval = setInterval(() => {
        const duration = Math.floor((Date.now() - this.startTime) / 1000);
        this.recordingStateSubject.next({
          isRecording: true,
          duration,
          audioBlob: null
        });
      }, 1000);

      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };

      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: mimeType });
        const duration = Math.floor((Date.now() - this.startTime) / 1000);
        
        this.recordingStateSubject.next({
          isRecording: false,
          duration,
          audioBlob
        });

        // Clean up
        this.releaseResources();
      };

      this.mediaRecorder.start();
      
      this.recordingStateSubject.next({
        isRecording: true,
        duration: 0,
        audioBlob: null
      });

    } catch (error) {
      console.error('Error starting recording:', error);
      this.releaseResources();
      
      if (error instanceof Error) {
        if (error.name === 'NotAllowedError') {
          throw new Error('Microphone permission denied. Please allow microphone access.');
        } else if (error.name === 'NotFoundError') {
          throw new Error('No microphone found. Please connect a microphone.');
        }
      }
      throw new Error('Failed to start recording. Please check your microphone.');
    }
  }

  stopRecording(): void {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
      
      if (this.timerInterval) {
        clearInterval(this.timerInterval);
        this.timerInterval = null;
      }
    }
  }

  cancelRecording(): void {
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
    }
    
    this.audioChunks = [];
    this.releaseResources();
    
    this.recordingStateSubject.next({
      isRecording: false,
      duration: 0,
      audioBlob: null
    });
  }

  private releaseResources(): void {
    // Stop all audio tracks to release microphone
    if (this.stream) {
      this.stream.getTracks().forEach(track => track.stop());
      this.stream = null;
    }

    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  private getSupportedMimeType(): string {
    // Try to find the best supported audio format
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/ogg;codecs=opus',
      'audio/ogg',
      'audio/mp4',
      'audio/wav'
    ];

    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        return type;
      }
    }

    // Fallback to default
    return 'audio/webm';
  }

  isRecording(): boolean {
    return this.recordingStateSubject.value.isRecording;
  }

  getFileExtension(mimeType: string): string {
    const map: Record<string, string> = {
      'audio/webm': 'webm',
      'audio/ogg': 'ogg',
      'audio/mp4': 'mp4',
      'audio/wav': 'wav',
      'audio/mpeg': 'mp3'
    };

    // Extract base mime type (without codecs)
    const baseMimeType = mimeType.split(';')[0];
    return map[baseMimeType] || 'webm';
  }
}

