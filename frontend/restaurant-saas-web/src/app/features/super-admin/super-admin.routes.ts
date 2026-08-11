import { Routes } from '@angular/router';

export const SUPER_ADMIN_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./tenants-list.component').then((m) => m.TenantsListComponent) },
  { path: 'incidents', loadComponent: () => import('./incidents.component').then((m) => m.IncidentsComponent) },
];
