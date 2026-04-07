import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-organisations',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './organisations.component.html',
  styleUrl: './organisations.component.css'
})
export class OrganisationsComponent implements OnInit {
  organisations: any[] = [];
  filteredOrganisations: any[] = [];
  loading = true;
  error = '';
  success = '';
  showForm = false;
  submitting = false;
  selectedDomaine = '';

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

  loadOrganisations() {
    this.loading = true;
    this.adminService.getOrganisations().subscribe({
      next: (data) => {
        this.organisations = data;
        this.filteredOrganisations = data;
        this.loading = false;
      },
      error: () => { this.error = 'Erreur de chargement'; this.loading = false; }
    });
  }

  filterByDomaine(domaine: string) {
    this.selectedDomaine = domaine;
    if (!domaine) {
      this.filteredOrganisations = this.organisations;
    } else {
      this.filteredOrganisations = this.organisations.filter(o => o.domaineOrganisation === domaine);
    }
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
}