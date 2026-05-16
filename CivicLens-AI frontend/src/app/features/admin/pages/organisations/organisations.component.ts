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
  downloadingPdf = false;
  selectedDomaine = '';
  selectedType = '';
  searchQuery = '';
  selectedOrganisation: any | null = null;
  detailsOpen = false;
  togglingId: string | null = null;
  editMode = false;
  saving = false;
  deleting = false;
  showPassword = false;
  confirmOpen = false;
  confirmTitle = '';
  confirmMessage = '';

  form: FormGroup;
  editForm: FormGroup;

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

    this.editForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      nomOrganisation: ['', Validators.required],
      typeOrganisation: ['', Validators.required],
      domaineOrganisation: ['', Validators.required],
      region: [''],
      nouveauMotDePasse: [''],
      motDePasseAdmin: ['']
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
    this.editMode = false;
    this.showPassword = false;
  }

  closeDetails(): void {
    this.detailsOpen = false;
    this.selectedOrganisation = null;
    this.editMode = false;
    this.showPassword = false;
    this.saving = false;
    this.deleting = false;
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

  startEdit(): void {
    const o = this.selectedOrganisation;
    if (!o) return;

    this.editMode = true;
    this.showPassword = false;
    this.error = '';
    this.success = '';

    this.editForm.reset({
      email: o.email ?? '',
      nomOrganisation: o.nomOrganisation ?? '',
      typeOrganisation: o.typeOrganisation ?? '',
      domaineOrganisation: o.domaineOrganisation ?? '',
      region: o.region ?? '',
      nouveauMotDePasse: '',
      motDePasseAdmin: ''
    });
  }

  cancelEdit(): void {
    this.editMode = false;
    this.showPassword = false;
    this.editForm.reset();
  }

  saveEdit(): void {
    if (!this.selectedOrganisation?.id) return;
    if (this.editForm.invalid) return;
    if (this.saving) return;

    this.saving = true;
    this.error = '';
    this.success = '';

    const payload = { ...this.editForm.value };
    if (payload.nouveauMotDePasse) {
      if (!payload.motDePasseAdmin) {
        this.error = "Mot de passe admin requis pour changer le mot de passe.";
        this.saving = false;
        return;
      }
    } else {
      delete payload.nouveauMotDePasse;
      delete payload.motDePasseAdmin;
    }

    this.adminService.updateOrganisationDetails(this.selectedOrganisation.id, payload).subscribe({
      next: (updated) => {
        const idx = this.organisations.findIndex((x) => x.id === this.selectedOrganisation?.id);
        if (idx >= 0) {
          this.organisations[idx] = { ...this.organisations[idx], ...updated };
        }
        this.selectedOrganisation = idx >= 0 ? this.organisations[idx] : { ...this.selectedOrganisation, ...updated };
        this.success = 'Organisation mise à jour avec succès.';
        this.saving = false;
        this.editMode = false;
        this.showPassword = false;
      },
      error: (err) => {
        this.error = typeof err?.error === 'string'
          ? err.error
          : (err?.error?.message ?? 'Erreur lors de la mise à jour.');
        this.saving = false;
      }
    });
  }

  deleteSelected(): void {
    const o = this.selectedOrganisation;
    if (!o?.id) return;
    if (this.deleting) return;
    this.confirmTitle = 'Supprimer cette organisation ?';
    this.confirmMessage = 'Cette action est définitive. Si elle est liée à des signalements, la suppression sera refusée.';
    this.confirmOpen = true;
  }

  closeConfirm(): void {
    if (this.deleting) return;
    this.confirmOpen = false;
  }

  confirmDelete(): void {
    const o = this.selectedOrganisation;
    if (!o?.id) return;
    if (this.deleting) return;

    this.deleting = true;
    this.error = '';
    this.success = '';

    this.adminService.deleteOrganisation(o.id).subscribe({
      next: () => {
        this.success = 'Organisation supprimée.';
        this.deleting = false;
        this.confirmOpen = false;
        this.closeDetails();
        this.loadOrganisations();
      },
      error: (err) => {
        this.error = typeof err?.error === 'string'
          ? err.error
          : (err?.error?.message ?? 'Impossible de supprimer cette organisation.');
        this.deleting = false;
        this.confirmOpen = false;
      }
    });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  downloadReport(): void {
    if (this.downloadingPdf) return;
    this.downloadingPdf = true;
    this.error = '';

    this.adminService.downloadOrganisationsPdf({
      domaine: this.selectedDomaine || undefined,
      type: this.selectedType || undefined,
      q: this.searchQuery?.trim() || undefined
    }).subscribe({
      next: (blob) => {
        const fileName = `rapport_organisations_${new Date().toISOString().slice(0, 16).replace(/[:T]/g, '')}.pdf`;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
        this.downloadingPdf = false;
      },
      error: () => {
        this.error = 'Impossible de télécharger le rapport PDF.';
        this.downloadingPdf = false;
      }
    });
  }
}
