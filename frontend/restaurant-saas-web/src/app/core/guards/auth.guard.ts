import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() ? true : router.createUrlTree(['/auth/login']);
};

export const superAdminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.currentUser()?.isSuperAdmin ? true : router.createUrlTree(['/']);
};

/** Route-data-driven permission gate: `data: { permission: Permissions.Menu.Manage }`. */
export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const required = route.data['permission'] as string | undefined;

  if (!required || auth.hasPermission(required)) return true;
  return router.createUrlTree(['/']);
};

export const tenantOnlyMatch: CanMatchFn = () => {
  const auth = inject(AuthService);
  return !auth.currentUser()?.isSuperAdmin;
};
