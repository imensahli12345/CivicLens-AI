import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent }
    ]
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/dashboard/dashboard.routes').then(m => m.DASHBOARD_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['ADMIN', 'SUPERADMIN'] },
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'service',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/service/service.routes').then(m => m.SERVICE_ROUTES)
  },
  {
    path: 'organisation-dashboard',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['ORGANISATION'] },
    loadComponent: () =>
      import('./features/organisation/pages/organisation-dashboard/organisation-dashboard.component').then(
        m => m.OrganisationDashboardComponent
      )
  },
  {
    path: 'organisation-signalements',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['ORGANISATION'] },
    loadComponent: () =>
      import('./features/organisation/pages/organisation-signalements/organisation-signalements.component').then(
        m => m.OrganisationSignalementsComponent
      )
  },
  {
    path: 'organisation-map',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['ORGANISATION'] },
    loadComponent: () =>
      import('./features/organisation/pages/organisation-map/organisation-map.component').then(
        m => m.OrganisationMapComponent
      )
  },
  {
    path: 'dashboard-bureau-et-service',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['BUREAUDECONTROLE'] },
    loadComponent: () =>
      import('./features/admin/pages/dashboard-bureau-et-service/dashboard-bureau-et-service.component').then(
        m => m.DashboardBureauEtServiceComponent
      )
  },
  { path: '**', redirectTo: '' }
];
