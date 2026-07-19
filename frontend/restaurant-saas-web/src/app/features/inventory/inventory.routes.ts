import { Routes } from '@angular/router';

export const INVENTORY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./stock-list.component').then((m) => m.StockListComponent) },
];
