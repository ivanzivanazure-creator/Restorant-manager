import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MenuCategory } from '../../core/models/domain.models';

export interface CreateCategoryRequest {
  name: string;
  sortOrder: number;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  currency: string;
  allergens: string[];
}

@Injectable({ providedIn: 'root' })
export class MenuService {
  constructor(private readonly http: HttpClient) {}

  getMenu(locationId: string, activeOnly = false): Observable<MenuCategory[]> {
    return this.http.get<MenuCategory[]>(`${environment.apiBaseUrl}/menu/locations/${locationId}`, {
      params: { activeOnly },
    });
  }

  createCategory(locationId: string, request: CreateCategoryRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/menu/locations/${locationId}/categories`, request);
  }

  createProduct(categoryId: string, request: CreateProductRequest): Observable<string> {
    return this.http.post<string>(`${environment.apiBaseUrl}/menu/categories/${categoryId}/products`, request);
  }

  setProductActive(productId: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${environment.apiBaseUrl}/menu/products/${productId}/active`, null, { params: { isActive } });
  }
}
