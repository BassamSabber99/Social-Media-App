import { Injectable, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, IRetryPolicy, RetryContext } from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../environment';
import { AuthService } from './auth.service';

export interface HubConfig {
  reconnectPolicy?: IRetryPolicy;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly authService = inject(AuthService);
  private readonly baseUrl = environment.apiUrl;
  
  private readonly hubConnections = new Map<string, HubConnection>();
  private readonly connectionStatusSubjects = new Map<string, BehaviorSubject<boolean>>();
  
  /**
   * Creates and initializes a hub connection
   * @param hubName The name of the hub (e.g., 'chat', 'video')
   * @param config Optional configuration for the hub connection
   */
  createHubConnection(hubName: string, config?: HubConfig): void {
    try {
      // Check if connection already exists
      if (this.hubConnections.has(hubName)) {
        console.warn(`Hub connection '${hubName}' already exists`);
        return;
      }

      // Get existing status subject or create a new one
      let statusSubject = this.connectionStatusSubjects.get(hubName);
      if (!statusSubject) {
        statusSubject = new BehaviorSubject<boolean>(false);
        this.connectionStatusSubjects.set(hubName, statusSubject);
      }

      const token = this.authService.getToken();
      if (!token) {
        console.warn(`No auth token available, skipping ${hubName} hub initialization`);
        return;
      }

      const hubUrl = `${this.baseUrl.replace('/api', '')}/hubs/${hubName}?access_token=${token}`;
      
      const connectionBuilder = new HubConnectionBuilder().withUrl(hubUrl);
      
      // Apply reconnect policy if provided, otherwise use default
      if (config?.reconnectPolicy) {
        connectionBuilder.withAutomaticReconnect(config.reconnectPolicy);
      } else {
        connectionBuilder.withAutomaticReconnect();
      }
      
      const hubConnection = connectionBuilder.build();
      
      // Setup connection lifecycle handlers
      hubConnection.onclose(() => {
        console.log(`${hubName} hub connection closed`);
        statusSubject.next(false);
      });

      hubConnection.onreconnecting(() => {
        console.log(`${hubName} hub reconnecting...`);
        statusSubject.next(false);
      });

      hubConnection.onreconnected(() => {
        console.log(`${hubName} hub reconnected`);
        statusSubject.next(true);
      });

      // Store the connection
      this.hubConnections.set(hubName, hubConnection);
      
      // Start the connection
      hubConnection
        .start()
        .then(() => {
          console.log(`${hubName} hub connected successfully`);
          statusSubject.next(true);
        })
        .catch(error => {
          console.error(`${hubName} hub connection error:`, error);
          statusSubject.next(false);
        });
    } catch (error) {
      console.error(`Failed to initialize ${hubName} hub:`, error);
    }
  }

  /**
   * Registers an event handler for a specific hub
   * @param hubName The name of the hub
   * @param eventName The name of the event to listen for
   * @param handler The handler function to execute when the event is received
   */
  on<T = any>(hubName: string, eventName: string, handler: (...args: any[]) => void): void {
    const connection = this.hubConnections.get(hubName);
    if (!connection) {
      console.warn(`Cannot register event '${eventName}': Hub '${hubName}' not initialized`);
      return;
    }
    
    connection.on(eventName, handler);
  }

  /**
   * Invokes a method on the hub server
   * @param hubName The name of the hub
   * @param methodName The name of the method to invoke
   * @param args Arguments to pass to the method
   */
  async invoke(hubName: string, methodName: string, ...args: any[]): Promise<any> {
    const connection = this.hubConnections.get(hubName);
    if (!connection) {
      throw new Error(`Hub '${hubName}' not initialized`);
    }
    
    if (connection.state !== HubConnectionState.Connected) {
      throw new Error(`Hub '${hubName}' is not connected (state: ${connection.state})`);
    }
    
    try {
      return await connection.invoke(methodName, ...args);
    } catch (error) {
      console.error(`Error invoking '${methodName}' on hub '${hubName}':`, error);
      throw error;
    }
  }

  /**
   * Gets the connection status observable for a specific hub
   * @param hubName The name of the hub
   */
  getConnectionStatus$(hubName: string): Observable<boolean> {
    let statusSubject = this.connectionStatusSubjects.get(hubName);
    if (!statusSubject) {
      // Create a new subject if it doesn't exist
      statusSubject = new BehaviorSubject<boolean>(false);
      this.connectionStatusSubjects.set(hubName, statusSubject);
    }
    return statusSubject.asObservable();
  }

  /**
   * Checks if a hub is currently connected
   * @param hubName The name of the hub
   */
  isConnected(hubName: string): boolean {
    const connection = this.hubConnections.get(hubName);
    return connection?.state === HubConnectionState.Connected;
  }

  /**
   * Gets the current connection state of a hub
   * @param hubName The name of the hub
   */
  getConnectionState(hubName: string): string {
    const connection = this.hubConnections.get(hubName);
    return connection?.state || 'Disconnected';
  }

  /**
   * Waits for a hub to be connected
   * @param hubName The name of the hub
   * @param maxWaitTime Maximum time to wait in milliseconds
   */
  async ensureConnected(hubName: string, maxWaitTime: number = 5000): Promise<void> {
    const connection = this.hubConnections.get(hubName);
    if (!connection) {
      throw new Error(`Hub '${hubName}' not initialized`);
    }
    
    if (this.isConnected(hubName)) {
      return;
    }
    
    const startTime = Date.now();
    
    while (!this.isConnected(hubName) && (Date.now() - startTime) < maxWaitTime) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    
    if (!this.isConnected(hubName)) {
      throw new Error(`${hubName} hub is not connected. Please refresh the page and try again.`);
    }
  }

  /**
   * Disconnects a specific hub
   * @param hubName The name of the hub to disconnect
   */
  disconnect(hubName: string): void {
    const connection = this.hubConnections.get(hubName);
    if (connection) {
      connection.stop();
      this.hubConnections.delete(hubName);
      
      const statusSubject = this.connectionStatusSubjects.get(hubName);
      if (statusSubject) {
        statusSubject.next(false);
        statusSubject.complete();
        this.connectionStatusSubjects.delete(hubName);
      }
    }
  }

  /**
   * Disconnects all hubs
   */
  disconnectAll(): void {
    this.hubConnections.forEach((connection, hubName) => {
      this.disconnect(hubName);
    });
  }
}

