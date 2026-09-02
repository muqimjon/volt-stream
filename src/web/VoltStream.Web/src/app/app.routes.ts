import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/auth/guards';
import { PermissionService } from './core/permissions/permission';
import { NAV_ITEMS } from './features/shell/nav';

const indexRedirect: CanActivateFn = () => {
  const permissions = inject(PermissionService);
  const item = NAV_ITEMS.find((i) => !i.permission || permissions.has(i.permission));
  return inject(Router).createUrlTree([item?.path ?? '/profile']);
};

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login').then((m) => m.LoginPage),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'sale',
        canActivate: [permissionGuard('sales.create')],
        loadComponent: () => import('./features/sale/sale').then((m) => m.SalePage),
      },
      {
        path: 'supply',
        canActivate: [permissionGuard('supplies.create')],
        loadComponent: () => import('./features/supply/supply').then((m) => m.SupplyPage),
      },
      {
        path: 'my-sales',
        canActivate: [permissionGuard('sales.view')],
        loadComponent: () => import('./features/my-sales/my-sales').then((m) => m.MySalesPage),
      },
      {
        path: 'my-supplies',
        canActivate: [permissionGuard('supplies.view')],
        loadComponent: () =>
          import('./features/my-supplies/my-supplies').then((m) => m.MySuppliesPage),
      },
      {
        path: 'stats',
        loadComponent: () => import('./features/stats/stats').then((m) => m.StatsPage),
      },
      {
        path: 'logs',
        canActivate: [permissionGuard('logs.view_own')],
        loadComponent: () => import('./features/logs/logs').then((m) => m.LogsPage),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then((m) => m.ProfilePage),
      },
      { path: '', pathMatch: 'full', canActivate: [indexRedirect], children: [] },
    ],
  },
  { path: '**', redirectTo: '' },
];
