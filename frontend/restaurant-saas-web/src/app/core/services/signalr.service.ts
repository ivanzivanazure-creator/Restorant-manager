import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { TokenStorageService } from '../auth/token-storage.service';

/** Thin wrapper around a SignalR HubConnection: handles auth (token in the query string, per
 * backend's JwtBearerEvents.OnMessageReceived), auto-reconnect, and location-group join/leave.
 * Feature services (KitchenService, OrdersRealtimeService) build on top of this. */
@Injectable({ providedIn: 'root' })
export class SignalRHubClient {
  constructor(private readonly tokens: TokenStorageService) {}

  connect(hubUrl: string): signalR.HubConnection {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.tokens.accessToken ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.start().catch((err) => console.error(`SignalR connection to ${hubUrl} failed`, err));
    return connection;
  }

  onEvent<T>(connection: signalR.HubConnection, methodName: string): Observable<T> {
    return new Observable<T>((subscriber) => {
      const handler = (payload: T) => subscriber.next(payload);
      connection.on(methodName, handler);
      return () => connection.off(methodName, handler);
    });
  }

  async joinLocation(connection: signalR.HubConnection, locationId: string): Promise<void> {
    if (connection.state !== signalR.HubConnectionState.Connected) {
      await new Promise<void>((resolve) => {
        connection.onreconnected(() => resolve());
        if (connection.state === signalR.HubConnectionState.Connected) resolve();
      });
    }
    await connection.invoke('JoinLocation', locationId);
  }
}
