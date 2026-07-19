import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order, OrderSource, PaymentMethod, RestaurantTable } from '../../core/models/domain.models';

export interface AddOrderItemRequest {
  productVariantId: string;
  quantity: number;
  notes: string | null;
  modifiers: { modifierId: string }[];
}

@Injectable({ providedIn: 'root' })
export class PosService {
  constructor(private readonly http: HttpClient) {}

  getTables(locationId: string): Observable<RestaurantTable[]> {
    return this.http.get<RestaurantTable[]>(`${environment.apiBaseUrl}/restaurant-management/locations/${locationId}/tables`);
  }

  getOpenOrders(locationId: string): Observable<Order[]> {
    return this.http.get<Order[]>(`${environment.apiBaseUrl}/pos/locations/${locationId}/orders/open`);
  }

  getOrder(orderId: string): Observable<Order> {
    return this.http.get<Order>(`${environment.apiBaseUrl}/pos/orders/${orderId}`);
  }

  openOrder(locationId: string, tableId: string | null, serverEmployeeId: string, source: OrderSource = 'Pos'): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/pos/orders`, { locationId, tableId, serverEmployeeId, source });
  }

  addItem(orderId: string, request: AddOrderItemRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/pos/orders/${orderId}/items`, request);
  }

  removeItem(orderId: string, orderItemId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/pos/orders/${orderId}/items/${orderItemId}`);
  }

  applyDiscount(orderId: string, type: string, amountOff: number, reason: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/pos/orders/${orderId}/discounts`, { type, amountOff, reason });
  }

  addTip(orderId: string, amount: number): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/pos/orders/${orderId}/tip`, null, { params: { amount } });
  }

  sendToKitchen(orderId: string, warehouseId: string, targetCookMinutes = 15): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/pos/orders/${orderId}/send-to-kitchen`, null, {
      params: { warehouseId, targetCookMinutes },
    });
  }

  pay(orderId: string, method: PaymentMethod, amount: number, reference?: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/pos/orders/${orderId}/payments`, { method, amount, reference });
  }

  splitOrder(orderId: string, orderItemIds: string[]): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/pos/orders/${orderId}/split`, orderItemIds);
  }

  mergeOrders(targetOrderId: string, sourceOrderId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/pos/orders/${targetOrderId}/merge/${sourceOrderId}`, null);
  }
}
