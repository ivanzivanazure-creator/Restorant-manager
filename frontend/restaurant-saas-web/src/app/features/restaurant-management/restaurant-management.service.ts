import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Location, RestaurantTable, TableShape } from '../../core/models/domain.models';

export interface CreateLocationRequest {
  name: string;
  addressLine1: string;
  city: string;
  country: string;
  currency: string;
}

export interface CreateTableRequest {
  label: string;
  capacity: number;
  shape: TableShape;
  x: number;
  y: number;
}

@Injectable({ providedIn: 'root' })
export class RestaurantManagementService {
  constructor(private readonly http: HttpClient) {}

  getLocations(): Observable<Location[]> {
    return this.http.get<Location[]>(`${environment.apiBaseUrl}/restaurant-management/locations`);
  }

  createLocation(restaurantId: string, request: CreateLocationRequest): Observable<Location> {
    return this.http.post<Location>(`${environment.apiBaseUrl}/restaurant-management/restaurants/${restaurantId}/locations`, request);
  }

  getTables(locationId: string): Observable<RestaurantTable[]> {
    return this.http.get<RestaurantTable[]>(`${environment.apiBaseUrl}/restaurant-management/locations/${locationId}/tables`);
  }

  createTable(locationId: string, request: CreateTableRequest): Observable<RestaurantTable> {
    return this.http.post<RestaurantTable>(`${environment.apiBaseUrl}/restaurant-management/locations/${locationId}/tables`, request);
  }

  generateQrCode(tableId: string, selfOrderBaseUrl: string): Observable<RestaurantTable> {
    return this.http.post<RestaurantTable>(`${environment.apiBaseUrl}/restaurant-management/tables/${tableId}/qr-code`, null, {
      params: { selfOrderBaseUrl },
    });
  }
}
