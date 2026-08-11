import { Routes } from '@angular/router';
import { authGuard, superAdminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    // Public status page — deliberately outside authGuard; anyone (including logged-out prospects
    // checking uptime before signing up) can view it.
    path: 'status',
    loadComponent: () => import('./features/status/status-page.component').then((m) => m.StatusPageComponent),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    loadComponent: () => import('./features/shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'super-admin',
        canActivate: [superAdminGuard],
        loadChildren: () => import('./features/super-admin/super-admin.routes').then((m) => m.SUPER_ADMIN_ROUTES),
      },
      {
        path: 'restaurant-management',
        loadChildren: () =>
          import('./features/restaurant-management/restaurant-management.routes').then((m) => m.RESTAURANT_MANAGEMENT_ROUTES),
      },
      {
        path: 'menu',
        loadChildren: () => import('./features/menu/menu.routes').then((m) => m.MENU_ROUTES),
      },
      {
        path: 'pos',
        loadChildren: () => import('./features/pos/pos.routes').then((m) => m.POS_ROUTES),
      },
      {
        path: 'kitchen-display',
        loadComponent: () => import('./features/kitchen-display/kitchen-display.component').then((m) => m.KitchenDisplayComponent),
      },
      {
        path: 'inventory',
        loadChildren: () => import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
      },
      {
        path: 'billing',
        loadChildren: () => import('./features/billing/billing.routes').then((m) => m.BILLING_ROUTES),
      },
      {
        path: 'integrations',
        loadChildren: () => import('./features/integrations/integrations.routes').then((m) => m.INTEGRATIONS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
