import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActorDashboardTopbarComponent } from '../../../../shared/components/actor-dashboard-topbar/actor-dashboard-topbar.component';
import { SignalementsComponent } from '../../../../shared/components/signalements/signalements.component';
import { BureauSignalementFilters } from '../../../../core/services/bureau-signalements.service';

@Component({
  selector: 'app-organisation-signalements',
  standalone: true,
  imports: [CommonModule, FormsModule, ActorDashboardTopbarComponent, SignalementsComponent],
  templateUrl: './organisation-signalements.component.html',
  styleUrl: './organisation-signalements.component.css'
})
export class OrganisationSignalementsComponent implements OnInit {
  userName = 'CivicLens Organisation';
  loading = true;

  filterStatut = '';
  filterGravite = '';
  filterPriorite = '';
  activeFilters: BureauSignalementFilters | null = null;

  ngOnInit(): void {
    this.userName = this.resolveUserName();
    this.onFiltersChanged();
    this.loading = false;
  }

  onFiltersChanged(): void {
    this.activeFilters = this.buildFilters();
  }

  resetFilters(): void {
    this.filterStatut = '';
    this.filterGravite = '';
    this.filterPriorite = '';
    this.onFiltersChanged();
  }

  private buildFilters(): BureauSignalementFilters | null {
    const filters: BureauSignalementFilters = {
      statut: this.filterStatut.trim() || null,
      gravite: this.filterGravite.trim() || null,
      priorite: this.filterPriorite.trim() || null
    };

    const hasAny = Object.values(filters).some((value) => !!String(value ?? '').trim());
    return hasAny ? filters : null;
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
