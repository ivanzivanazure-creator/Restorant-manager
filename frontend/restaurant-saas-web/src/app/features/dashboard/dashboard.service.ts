import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardSummary } from '../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  getSummary(locationId: string): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${environment.apiBaseUrl}/dashboard/locations/${locationId}/summary`);
  }
}
