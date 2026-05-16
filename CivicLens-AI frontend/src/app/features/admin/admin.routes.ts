import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/statistiques/statistiques.component').then(m => m.StatistiquesComponent)
      },
      {
        path: 'citoyens',
        loadComponent: () =>
          import('./pages/citoyens/citoyens.component').then(m => m.CitoyensComponent)
      },
      {
        path: 'bureaux',
        loadComponent: () =>
          import('./pages/bureaux/bureaux.component').then(m => m.BureauxComponent)
      },
      {
        path: 'organisations',
        loadComponent: () =>
          import('./pages/organisations/organisations.component').then(m => m.OrganisationsComponent)
      },
      {
        path: 'reclamations',
        loadComponent: () =>
          import('./pages/reclamations/reclamations.component').then(m => m.ReclamationsComponent)
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/platform-notifications/platform-notifications.component').then(m => m.PlatformNotificationsComponent)
      },
      {
        path: 'activation-requests',
        loadComponent: () =>
          import('./pages/activation-requests/activation-requests.component').then(m => m.ActivationRequestsComponent)
      },
      {
        path: 'statistiques',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  }
];
