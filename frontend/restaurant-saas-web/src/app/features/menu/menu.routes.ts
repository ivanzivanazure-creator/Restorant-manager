import { Routes } from '@angular/router';

export const MENU_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./menu-list.component').then((m) => m.MenuListComponent) },
];
