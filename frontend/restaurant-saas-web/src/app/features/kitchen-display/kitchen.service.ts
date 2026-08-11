import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { KitchenTicket, KitchenTicketPriority } from '../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class KitchenService {
  constructor(private readonly http: HttpClient) {}

  getQueue(locationId: string): Observable<KitchenTicket[]> {
    return this.http.get<KitchenTicket[]>(`${environment.apiBaseUrl}/kitchen/locations/${locationId}/queue`);
  }

  start(ticketId: string): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/kitchen/tickets/${ticketId}/start`, null);
  }

  markReady(ticketId: string): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/kitchen/tickets/${ticketId}/ready`, null);
  }

  markServed(ticketId: string): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/kitchen/tickets/${ticketId}/served`, null);
  }

  setPriority(ticketId: string, priority: KitchenTicketPriority): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/kitchen/tickets/${ticketId}/priority`, null, { params: { priority } });
  }
}
