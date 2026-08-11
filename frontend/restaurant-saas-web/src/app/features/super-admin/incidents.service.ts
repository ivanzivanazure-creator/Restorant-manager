import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Incident, IncidentSeverity, IncidentStatus, PlatformComponentName } from '../../core/models/domain.models';

export interface CreateIncidentRequest {
  title: string;
  description: string;
  severity: IncidentSeverity;
  affectedComponents: PlatformComponentName[];
}

@Injectable({ providedIn: 'root' })
export class IncidentsService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<Incident[]> {
    return this.http.get<Incident[]>(`${environment.apiBaseUrl}/status/incidents`);
  }

  create(request: CreateIncidentRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/status/incidents`, request);
  }

  postUpdate(incidentId: string, status: IncidentStatus, message: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/status/incidents/${incidentId}/updates`, { status, message });
  }
}
