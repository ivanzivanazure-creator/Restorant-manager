import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StockLevel } from '../../core/models/domain.models';

export interface ReceiveStockRequest {
  warehouseId: string;
  ingredientId: string;
  quantity: number;
  unitCost: number;
  expiresAt: string | null;
}

export interface Warehouse {
  id: string;
  locationId: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private readonly http: HttpClient) {}

  getWarehouses(locationId: string): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${environment.apiBaseUrl}/inventory/locations/${locationId}/warehouses`);
  }

  getStock(warehouseId: string): Observable<StockLevel[]> {
    return this.http.get<StockLevel[]>(`${environment.apiBaseUrl}/inventory/warehouses/${warehouseId}/stock`);
  }

  getLowStockAlerts(): Observable<StockLevel[]> {
    return this.http.get<StockLevel[]>(`${environment.apiBaseUrl}/inventory/alerts/low-stock`);
  }

  receiveStock(request: ReceiveStockRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/inventory/stock/receive`, request);
  }
}
