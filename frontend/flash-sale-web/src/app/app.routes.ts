import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'buy',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/flash-sale/buy/buy.component').then((m) => m.BuyComponent)
  },
  {
    path: 'ops',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/devops/dashboard/devops-dashboard.component').then(
        (m) => m.DevOpsDashboardComponent
      )
  },
  { path: '', pathMatch: 'full', redirectTo: 'buy' },
  { path: '**', redirectTo: 'buy' }
];
