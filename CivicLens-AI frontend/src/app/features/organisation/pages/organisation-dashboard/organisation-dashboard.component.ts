import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActorDashboardTopbarComponent } from '../../../../shared/components/actor-dashboard-topbar/actor-dashboard-topbar.component';
import { SignalementsComponent } from '../../../../shared/components/signalements/signalements.component';
import { SimpleAlertComponent } from '../../../../shared/components/simple-alert/simple-alert.component';
import { AboutFooterComponent } from '../../../../shared/components/AboutFooterComponent/about-footer.component';
import { OrganisationSignalementsService } from '../../../../core/services/organisation-signalements.service';

@Component({
  selector: 'app-organisation-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ActorDashboardTopbarComponent,
    SignalementsComponent,
    SimpleAlertComponent,
    AboutFooterComponent
  ],
  templateUrl: './organisation-dashboard.component.html',
  styleUrl: './organisation-dashboard.component.css'
})
export class OrganisationDashboardComponent implements OnInit {
  userName = 'CivicLens Organisation';
  loading = true;
  showSignalementsModal = false;
  showAlert = false;
  alertType: 'success' | 'error' = 'error';
  alertMessage = '';
  private alertTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor(private organisationSignalementsService: OrganisationSignalementsService) {}

  ngOnInit(): void {
    this.userName = this.resolveUserName();
    this.loading = false;
  }

  onSignalementAction(action: 'view' | 'assign'): void {
    if (action !== 'view') return;
    this.openSignalementsModal();
  }

  openSignalementsModal(): void {
    this.showSignalementsModal = true;
    document.body.style.overflow = 'hidden';
  }

  closeSignalementsModal(): void {
    this.showSignalementsModal = false;
    document.body.style.overflow = '';
  }

  closeAlert(): void {
    this.showAlert = false;
    if (this.alertTimeoutId) {
      clearTimeout(this.alertTimeoutId);
      this.alertTimeoutId = null;
    }
  }

  private resolveUserName(): string {
    const keys = ['full_name', 'username', 'userName', 'nomOrganisation', 'nom', 'email'];
    for (const key of keys) {
      const value = localStorage.getItem(key);
      if (value && value.trim()) {
        return value.trim();
      }
    }
    return 'CivicLens Organisation';
  }
}
