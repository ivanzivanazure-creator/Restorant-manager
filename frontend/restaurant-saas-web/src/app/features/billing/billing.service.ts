import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BillingSummary } from '../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class BillingService {
  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<BillingSummary> {
    return this.http.get<BillingSummary>(`${environment.apiBaseUrl}/billing/summary`);
  }

  connectStripe(): Observable<{ onboardingUrl: string }> {
    const returnUrl = `${window.location.origin}/billing?connected=1`;
    const refreshUrl = `${window.location.origin}/billing`;
    return this.http.post<{ onboardingUrl: string }>(`${environment.apiBaseUrl}/billing/connect-stripe`, { returnUrl, refreshUrl });
  }
}
