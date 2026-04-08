import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../services/admin.service';
import { FormsModule } from '@angular/forms';
import { TUNISIA_REGIONS } from '../../../../shared/data/tunisia-regions';

@Component({
  selector: 'app-citoyens',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './citoyens.component.html',
  styleUrl: './citoyens.component.css'
})
export class CitoyensComponent implements OnInit {
  citoyens: any[] = [];
  loading = true;
  error = '';
  regions = Object.keys(TUNISIA_REGIONS);
  delegations: string[] = [];
  selectedRegion = '';
  selectedDelegation = '';
  selectedStatut = '';
  searchQuery = '';

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadCitoyens();
  }

  get filteredCitoyens(): any[] {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) return this.citoyens;
    return this.citoyens.filter((c) => {
      const fullName = `${c?.prenom ?? ''} ${c?.nom ?? ''}`.trim().toLowerCase();
      const email = (c?.email ?? '').toLowerCase();
      const cin = (c?.cin ?? '').toLowerCase();
      return fullName.includes(q) || email.includes(q) || cin.includes(q);
    });
  }

  onSearchChange(): void {
    // client-side only
  }

  onRegionChange(): void {
    this.delegations = this.selectedRegion
      ? (TUNISIA_REGIONS[this.selectedRegion as keyof typeof TUNISIA_REGIONS] || [])
      : [];
    this.selectedDelegation = '';
    this.loadCitoyens();
  }

  onDelegationChange(): void {
    this.loadCitoyens();
  }

  onStatutChange(): void {
    this.loadCitoyens();
  }

  resetFilters(): void {
    this.selectedRegion = '';
    this.selectedDelegation = '';
    this.selectedStatut = '';
    this.delegations = [];
    this.loadCitoyens();
  }

  private loadCitoyens(): void {
    this.loading = true;
    this.error = '';
    this.adminService.getCitoyens({
      region: this.selectedRegion || undefined,
      delegation: this.selectedDelegation || undefined,
      statut: this.selectedStatut || undefined
    }).subscribe({
      next: (data) => {
        this.citoyens = data;
        this.loading = false;
      },
      error: () => {
        this.error = 'Erreur de chargement';
        this.loading = false;
      }
    });
  }
}
