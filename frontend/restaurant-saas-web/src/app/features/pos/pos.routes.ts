import { Routes } from '@angular/router';

export const POS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./table-map.component').then((m) => m.TableMapComponent) },
  { path: 'order/:orderId', loadComponent: () => import('./order-screen.component').then((m) => m.OrderScreenComponent) },
];
