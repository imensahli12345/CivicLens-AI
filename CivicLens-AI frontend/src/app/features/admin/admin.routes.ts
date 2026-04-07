import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      { path: '', redirectTo: 'citoyens', pathMatch: 'full' },
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
        path: 'statistiques',
        loadComponent: () =>
          import('./pages/statistiques/statistiques.component').then(m => m.StatistiquesComponent)
      }
    ]
  }
];