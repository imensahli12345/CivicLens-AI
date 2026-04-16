import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { MesSignalementsComponent } from './pages/mes-signalements/mes-signalements.component';
import { NouveauSignalementComponent } from './pages/nouveau-signalement/nouveau-signalement.component';
import { RegionFeedComponent } from './pages/region-feed/region-feed.component';
import { ProfilComponent } from './pages/profil/profil.component';
import { ProfilEditComponent } from './pages/profil/profil-edit.component';
import { AboutComponent } from './pages/about/about.component';
import { SignalementDetailsComponent } from './pages/signalement-details/signalement-details.component';
import { EvenementsFeedComponent } from './pages/evenements-feed/evenements-feed.component';
import { NouveauEvenementComponent } from './pages/nouveau-evenement/nouveau-evenement.component';
import { EvenementDetailsComponent } from './pages/evenement-details/evenement-details.component';
import { EvenementParticipantsComponent } from './pages/evenement-participants/evenement-participants.component';
import { NotificationsComponent } from './pages/notifications/notifications.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardComponent,
    children: [
      { path: '', component: HomePageComponent },
      { path: 'mes-signalements', component: MesSignalementsComponent },
      { path: 'nouveau', component: NouveauSignalementComponent },
      { path: 'region', component: RegionFeedComponent },
      { path: 'evenements', component: EvenementsFeedComponent },
      { path: 'evenements/nouveau', component: NouveauEvenementComponent },
      { path: 'evenements/:id/participants', component: EvenementParticipantsComponent },
      { path: 'evenements/:id', component: EvenementDetailsComponent },
      { path: 'notifications', component: NotificationsComponent },
      { path: 'profil/modifier', component: ProfilEditComponent },
      { path: 'profil', component: ProfilComponent },
      { path: 'about', component: AboutComponent },
      { path: 'signalement/:id', component: SignalementDetailsComponent }
    ]
  }
];