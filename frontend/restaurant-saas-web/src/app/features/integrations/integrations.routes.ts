import { Routes } from '@angular/router';

export const INTEGRATIONS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./delivery-integrations.component').then((m) => m.DeliveryIntegrationsComponent) },
];
