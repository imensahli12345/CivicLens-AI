import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ActorDashboardTopbarComponent } from '../../../../shared/components/actor-dashboard-topbar/actor-dashboard-topbar.component';
import { OrganisationSignalementsService } from '../../../../core/services/organisation-signalements.service';
import { BureauSignalement } from '../../../../core/services/bureau-signalements.service';

@Component({
  selector: 'app-organisation-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ActorDashboardTopbarComponent
  ],
  templateUrl: './organisation-dashboard.component.html',
  styleUrl: './organisation-dashboard.component.css'
})
export class OrganisationDashboardComponent implements OnInit {
  userName = 'CivicLens Organisation';
  loading = true;
  error: string | null = null;

  totalSignalements = 0;
  aTraiter = 0;
  enCoursEtude = 0;
  resolus = 0;
  derniersSignalements: BureauSignalement[] = [];

  constructor(private organisationSignalements: OrganisationSignalementsService) {}

  ngOnInit(): void {
    this.userName = this.resolveUserName();
    this.loadDashboard();
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  getStatutClass(statut: string): string {
    const map: Record<string, string> = {
      ENVOYEE: 'badge-envoyee',
      EN_COURS: 'badge-encours',
      EN_COURS_DETUDE: 'badge-encours',
      OUVERTE: 'badge-encours',
      RESOLU: 'badge-resolue',
      RESOLUE: 'badge-resolue',
      REJETEE: 'badge-rejetee',
      REJETE: 'badge-rejetee'
    };
    return map[statut] || 'badge-default';
  }

  private loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.organisationSignalements.getAllSignalements().subscribe({
      next: (items) => {
        const list = items ?? [];
        this.totalSignalements = list.length;
        this.aTraiter = list.filter((s) => s.statut === 'ENVOYEE' || s.statut === 'OUVERTE').length;
        this.enCoursEtude = list.filter((s) => s.statut === 'EN_COURS_DETUDE' || s.statut === 'EN_COURS').length;
        this.resolus = list.filter((s) => s.statut === 'RESOLU' || s.statut === 'RESOLUE').length;

        this.derniersSignalements = [...list]
          .sort((a, b) => new Date(b.dateCreation).getTime() - new Date(a.dateCreation).getTime())
          .slice(0, 5);

        this.loading = false;
      },
      error: () => {
        this.error = 'Impossible de charger les donnees du tableau de bord.';
        this.loading = false;
      }
    });
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
