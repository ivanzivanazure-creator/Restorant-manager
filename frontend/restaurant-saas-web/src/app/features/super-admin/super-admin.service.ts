import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PaginatedList, PlatformAnalytics, TenantSummary } from '../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  constructor(private readonly http: HttpClient) {}

  getTenants(pageNumber = 1, pageSize = 20, search?: string): Observable<PaginatedList<TenantSummary>> {
    return this.http.get<PaginatedList<TenantSummary>>(`${environment.apiBaseUrl}/super-admin/tenants`, {
      params: { pageNumber, pageSize, ...(search ? { search } : {}) },
    });
  }

  getAnalytics(): Observable<PlatformAnalytics> {
    return this.http.get<PlatformAnalytics>(`${environment.apiBaseUrl}/super-admin/analytics`);
  }

  deactivateTenant(tenantId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/super-admin/tenants/${tenantId}/deactivate`, null);
  }
}
