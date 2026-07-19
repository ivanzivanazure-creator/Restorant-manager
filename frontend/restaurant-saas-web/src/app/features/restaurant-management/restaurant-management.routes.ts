import { Routes } from '@angular/router';

export const RESTAURANT_MANAGEMENT_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./locations.component').then((m) => m.LocationsComponent) },
];
