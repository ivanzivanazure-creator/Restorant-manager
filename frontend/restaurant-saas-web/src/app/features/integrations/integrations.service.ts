import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DeliveryIntegration, DeliveryPlatform, RegisterDeliveryIntegrationResponse } from '../../core/models/domain.models';

export interface RegisterDeliveryIntegrationRequest {
  locationId: string;
  platform: DeliveryPlatform;
  externalStoreId: string | null;
}

@Injectable({ providedIn: 'root' })
export class IntegrationsService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<DeliveryIntegration[]> {
    return this.http.get<DeliveryIntegration[]>(`${environment.apiBaseUrl}/integrations/delivery`);
  }

  register(request: RegisterDeliveryIntegrationRequest): Observable<RegisterDeliveryIntegrationResponse> {
    return this.http.post<RegisterDeliveryIntegrationResponse>(`${environment.apiBaseUrl}/integrations/delivery`, request);
  }

  deactivate(integrationId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/integrations/delivery/${integrationId}/deactivate`, null);
  }
}
