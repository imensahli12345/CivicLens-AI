import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
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
    path: 'statistiques',
    loadComponent: () =>
      import('./pages/statistiques/statistiques.component').then(m => m.StatistiquesComponent)
  }
];