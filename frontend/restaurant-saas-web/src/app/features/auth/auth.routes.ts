import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: 'login', loadComponent: () => import('./login.component').then((m) => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./register.component').then((m) => m.RegisterComponent) },
  { path: 'mfa', loadComponent: () => import('./mfa-challenge.component').then((m) => m.MfaChallengeComponent) },
  {
    path: 'forgot-password',
    loadComponent: () => import('./forgot-password.component').then((m) => m.ForgotPasswordComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'login' },
];
