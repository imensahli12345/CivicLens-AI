import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-organisations',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './organisations.component.html',
  styleUrl: './organisations.component.css'
})
export class OrganisationsComponent implements OnInit {
  organisations: any[] = [];
  loading = true;
  error = '';
  success = '';
  showForm = false;
  submitting = false;
  selectedDomaine = '';
  selectedType = '';
  searchQuery = '';
  selectedOrganisation: any | null = null;
  detailsOpen = false;
  togglingId: string | null = null;

  form: FormGroup;

  typesOrganisation = ['MINISTERE', 'COLLECTIVITE_TERRITORIALE', 'ENTREPRISE_PUBLIQUE', 'ONG', 'AUTRE'];
  domaines = ['ROUTE', 'ECLAIRAGE', 'EAU', 'TRANSPORT', 'ELECTRICITE', 'INFRASTRUCTURE', 'DECHETS', 'AUTRE'];
  regions = [
    'Tunis', 'Ariana', 'Ben Arous', 'Manouba', 'Nabeul', 'Zaghouan',
    'Bizerte', 'Béja', 'Jendouba', 'Kef', 'Siliana', 'Sousse',
    'Monastir', 'Mahdia', 'Sfax', 'Kairouan', 'Kasserine', 'Sidi Bouzid',
    'Gabès', 'Medenine', 'Tataouine', 'Gafsa', 'Tozeur', 'Kébili'
  ];

  constructor(private adminService: AdminService, private fb: FormBuilder) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      motDePasse: ['', Validators.required],
      nomOrganisation: ['', Validators.required],
      typeOrganisation: ['', Validators.required],
      domaineOrganisation: ['', Validators.required],
      region: ['']
    });
  }

  ngOnInit() {
    this.loadOrganisations();
  }

  get filteredOrganisations(): any[] {
    const q = this.searchQuery.trim().toLowerCase();
    if (!q) return this.organisations;
    return this.organisations.filter((o) => {
      const name = (o?.nomOrganisation ?? '').toLowerCase();
      const email = (o?.email ?? '').toLowerCase();
      return name.includes(q) || email.includes(q);
    });
  }

  loadOrganisations() {
    this.loading = true;
    this.adminService.getOrganisationsFiltered({
      domaine: this.selectedDomaine || undefined,
      type: this.selectedType || undefined
    }).subscribe({
      next: (data) => {
        this.organisations = data;
        this.loading = false;
      },
      error: () => { this.error = 'Erreur de chargement'; this.loading = false; }
    });
  }

  onDomaineChange(): void {
    this.loadOrganisations();
  }

  onTypeChange(): void {
    this.loadOrganisations();
  }

  resetFilters(): void {
    this.selectedDomaine = '';
    this.selectedType = '';
    this.searchQuery = '';
    this.loadOrganisations();
  }

  toggleForm() {
    this.showForm = !this.showForm;
    this.success = '';
    this.error = '';
    this.form.reset();
  }

  submit() {
    if (this.form.invalid) return;
    this.submitting = true;
    this.adminService.createOrganisation(this.form.value).subscribe({
      next: () => {
        this.success = 'Organisation créée avec succès !';
        this.submitting = false;
        this.showForm = false;
        this.form.reset();
        this.loadOrganisations();
      },
      error: (err) => {
        this.error = typeof err.error === 'string' ? err.error : 'Erreur lors de la création';
        this.submitting = false;
      }
    });
  }

  openDetails(org: any): void {
    this.selectedOrganisation = org;
    this.detailsOpen = true;
  }

  closeDetails(): void {
    this.detailsOpen = false;
    this.selectedOrganisation = null;
  }

  toggleOrganisation(org: any): void {
    if (!org?.id) return;
    const next = org.statut === 'ACTIF' ? 'INACTIF' : 'ACTIF';
    const prev = org.statut;
    this.togglingId = org.id;
    org.statut = next;
    this.adminService.updateOrganisationStatut(org.id, next).subscribe({
      next: (res) => {
        org.statut = res?.statut ?? next;
        this.togglingId = null;
      },
      error: () => {
        org.statut = prev;
        this.togglingId = null;
      }
    });
  }
}
