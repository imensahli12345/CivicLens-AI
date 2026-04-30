import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/role.guard';

export const SERVICE_ROUTES: Routes = [
  {
    path: '',
    canActivate: [roleGuard],
    data: { roles: ['SERVICE'] },
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard-service/dashboard-service.component').then(m => m.DashboardServiceComponent)
      }
    ]
  }
];
