import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';
import { PermissionService } from '../permissions/permission';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.checkExpiry() || router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return !auth.checkExpiry() || router.createUrlTree(['/']);
};

export const permissionGuard =
  (permission: string | string[]): CanActivateFn =>
  () => {
    const router = inject(Router);
    return inject(PermissionService).has(permission) || router.createUrlTree(['/']);
  };
