import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-bureaux',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './bureaux.component.html',
  styleUrl: './bureaux.component.css'
})
export class BureauxComponent implements OnInit {
  bureaux: any[] = [];
  loading = true;
  error = '';
  success = '';
  showForm = false;
  submitting = false;
  downloadingPdf = false;
  delegations: string[] = [];
  editDelegations: string[] = [];
  filterRegion = '';
  filterDelegation = '';
  filterDelegations: string[] = [];
  selectedBureau: any | null = null;
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

  regionDelegations: { [key: string]: string[] } = {
    'Tunis': ['Tunis', 'Carthage', 'Le Bardo', 'La Goulette', 'Le Kram', 'La Marsa', 'Kalâat el-Andalous', 'Sidi Hassine', 'Ezzouhour', 'Djebel Jelloud', 'Bab El Bhar', 'Bab Souika', 'Cité El Khadra', 'El Kabaria', 'El Menzah', 'El Omrane', 'El Omrane Supérieur', 'El Ouardia', 'Ettahrir', 'Hrairia', 'Medina', 'Séjoumi'],
    'Ariana': ['Ariana Ville', 'Ettadhamen', 'Kalâat el-Andalous', 'La Soukra', 'Mnihla', 'Raoued', 'Sidi Thabet'],
    'Ben Arous': ['Ben Arous', 'Bou Mhel el-Bassatine', 'El Mourouj', 'Ezzahra', 'Fouchana', 'Hammam Chott', 'Hammam Lif', 'Khalidia', 'Medina Jedida', 'Mégrine', 'Mohamedia', 'Nouvelle Médina', 'Radès'],
    'Manouba': ['Manouba', 'Den Den', 'Douar Hicher', 'El Battan', 'Jedaida', 'Mornaguia', 'Oued Ellil', 'Tébourba'],
    'Nabeul': ['Nabeul', 'Béni Khalled', 'Béni Khiar', 'Bou Argoub', 'Dar Chaâbane El Fehri', 'El Haouaria', 'El Mida', 'Grombalia', 'Hammam Ghezèze', 'Hammamet', 'Kelibia', 'Korba', 'Menzel Bouzelfa', 'Menzel Temime', 'Soliman', 'Takelsa'],
    'Zaghouan': ['Zaghouan', 'Bir Mcherga', 'El Fahs', 'En Nadhour', 'Saouaf', 'Zriba'],
    'Bizerte': ['Bizerte Nord', 'Bizerte Sud', 'Ghezala', 'Ghar El Melh', 'Joumine', 'Mateur', 'Menzel Bourguiba', 'Menzel Jemil', 'Ras Jebel', 'Sejnane', 'Tinja', 'Utique', 'El Alia'],
    'Béja': ['Béja Nord', 'Béja Sud', 'Amdoun', 'Goubellat', 'Medjez el-Bab', 'Nefza', 'Téboursouk', 'Testour', 'Thibar'],
    'Jendouba': ['Jendouba', 'Jendouba Nord', 'Aïn Draham', 'Balta-Bou Aouane', 'Bou Salem', 'Fernana', 'Ghardimaou', 'Oued Meliz', 'Tabarka'],
    'Kef': ['Le Kef Ouest', 'Le Kef Est', 'Dahmani', 'Es Sers', 'Jerissa', 'Kalâat Sinane', 'Kalâat Khasba', 'Nebeur', 'Sakiet Sidi Youssef', 'Tajerouine'],
    'Siliana': ['Siliana Nord', 'Siliana Sud', 'Bargou', 'Bou Arada', 'El Aroussa', 'El Krib', 'Gaâfour', 'Kesra', 'Makthar', 'Rouhia'],
    'Sousse': ['Sousse Médina', 'Sousse Riadh', 'Sousse Jawhara', 'Sousse Sidi Abdelhamid', 'Akouda', 'Bouficha', 'Enfidha', 'Hammam Sousse', 'Hergla', 'Kalâa Kebira', 'Kalâa Seghira', 'Kondar', 'Msaken', 'Sidi Bou Ali', 'Sidi El Hani'],
    'Monastir': ['Monastir', 'Bekalta', 'Bembla', 'Beni Hassen', 'Bouhjar', 'Jammel', 'Ksar Hellal', 'Ksibet el-Mediouni', 'Ouerdanine', 'Sahline', 'Sayada-Lamta-Bouhjar', 'Skanes', 'Téboulba', 'Zeramdine'],
    'Mahdia': ['Mahdia', 'Bou Merdes', 'Chebba', 'Chegarnia', 'Chorbane', 'El Bradaa', 'Essouassi', 'Hbira', 'Ksour Essef', 'Melloulèche', 'Ouled Chamekh', 'Sidi Alouane'],
    'Sfax': ['Sfax Ville', 'Sfax Ouest', 'Sfax Sud', 'Agareb', 'Bir Ali Ben Khalifa', 'Djebeniana', 'El Amra', 'El Ghraiba', 'Hencha', 'Kerkennah', 'Mahras', 'Menzel Chaker', 'Sakiet Eddaier', 'Sakiet Ezzit', 'Skhira'],
    'Kairouan': ['Kairouan Nord', 'Kairouan Sud', 'Bouhajla', 'Chebika', 'Cherarda', 'El Alaa', 'Haffouz', 'Hajeb El Ayoun', 'Nasrallah', 'Oueslatia', 'Sbikha'],
    'Kasserine': ['Kasserine Nord', 'Kasserine Sud', 'Aïn Jedey', 'El Ayoun', 'Fériana', 'Foussana', 'Hassi El Ferid', 'Hidra', 'Jedeliane', 'Majel Bel Abbès', 'Sbeitla', 'Sbiba', 'Thala'],
    'Sidi Bouzid': ['Sidi Bouzid Ouest', 'Sidi Bouzid Est', 'Ben Aoun', 'Bir El Hafey', 'Cebbala Ouled Asker', 'Jilma', 'Mazzouna', 'Meknassy', 'Menzel Bouzaiane', 'Ouled Haffouz', 'Regueb', 'Souk Jedid'],
    'Gabès': ['Gabès Médina', 'Gabès Ouest', 'Gabès Sud', 'El Hamma', 'Ghannouch', 'Mareth', 'Matmata', 'Matmata Nouvelle', 'Menzel El Habib', 'Métouia', 'Nouvelle Matmata', 'Oudhref'],
    'Medenine': ['Medenine Nord', 'Medenine Sud', 'Ben Gardane', 'Beni Khedache', 'Djerba Ajim', 'Djerba Houmt Souk', 'Djerba Midoun', 'Sidi Makhlouf', 'Zarzis'],
    'Tataouine': ['Tataouine Nord', 'Tataouine Sud', 'Bir Lahmar', 'Dehiba', 'Ghomrassen', 'Remada', 'Smar'],
    'Gafsa': ['Gafsa Nord', 'Gafsa Sud', 'Belkhir', 'El Guettar', 'El Ksar', 'Mdhilla', 'Métlaoui', 'Moularès', 'Redeyef', 'Sened', 'Sidi Aïch'],
    'Tozeur': ['Tozeur', 'Degache', 'Hazoua', 'Nefta', 'Tameghza'],
    'Kébili': ['Kébili Nord', 'Kébili Sud', 'Douz Nord', 'Douz Sud', 'Faouar', 'Souk Lahad']
  };

  regions = Object.keys(this.regionDelegations);

  constructor(private adminService: AdminService, private fb: FormBuilder) {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      motDePasse: ['', Validators.required],
      matricule: ['', Validators.required],
      region: ['', Validators.required],
      delegation: ['', Validators.required]
    });

    this.editForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      matricule: ['', Validators.required],
      region: ['', Validators.required],
      delegation: ['', Validators.required],
      nouveauMotDePasse: [''],
      motDePasseAdmin: ['']
    });
  }

  ngOnInit() {
    this.loadBureaux();

    this.form.get('region')?.valueChanges.subscribe(region => {
      this.delegations = this.regionDelegations[region] || [];
      this.form.get('delegation')?.setValue('');
      this.form.get('email')?.setValue('');
    });

    this.form.get('delegation')?.valueChanges.subscribe(delegation => {
      if (delegation) {
        const normalized = delegation
          .toLowerCase()
          .normalize('NFD')
          .replace(/[\u0300-\u036f]/g, '')
          .replace(/\s+/g, '.')
          .replace(/[^a-z0-9.]/g, '');
        this.form.get('email')?.setValue(`bureau.${normalized}@civiclens.tn`);
      }
    });

    this.editForm.get('region')?.valueChanges.subscribe(region => {
      this.editDelegations = this.regionDelegations[region] || [];
      const currentDelegation = this.editForm.get('delegation')?.value;
      if (currentDelegation && !this.editDelegations.includes(currentDelegation)) {
        this.editForm.get('delegation')?.setValue('');
      }
    });
  }

  loadBureaux() {
    this.loading = true;
    this.adminService.getBureauxFiltered({
      region: this.filterRegion || undefined,
      delegation: this.filterDelegation || undefined
    }).subscribe({
      next: (data) => { this.bureaux = data || []; this.loading = false; },
      error: () => { this.error = 'Erreur de chargement'; this.loading = false; }
    });
  }

  onFilterRegionChange(): void {
    this.filterDelegations = this.filterRegion ? (this.regionDelegations[this.filterRegion] || []) : [];
    this.filterDelegation = '';
    this.loadBureaux();
  }

  onFilterDelegationChange(): void {
    this.loadBureaux();
  }

  resetFilters(): void {
    this.filterRegion = '';
    this.filterDelegation = '';
    this.filterDelegations = [];
    this.loadBureaux();
  }

  openDetails(bureau: any): void {
    this.selectedBureau = bureau;
    this.detailsOpen = true;
    this.editMode = false;
    this.showPassword = false;
  }

  closeDetails(): void {
    this.detailsOpen = false;
    this.selectedBureau = null;
    this.editMode = false;
    this.showPassword = false;
    this.saving = false;
    this.deleting = false;
  }

  toggleBureau(bureau: any): void {
    if (!bureau?.id) return;
    const next = bureau.statut === 'ACTIF' ? 'INACTIF' : 'ACTIF';
    const prev = bureau.statut;
    this.togglingId = bureau.id;
    bureau.statut = next;
    this.adminService.updateBureauStatut(bureau.id, next).subscribe({
      next: (res) => {
        bureau.statut = res?.statut ?? next;
        this.togglingId = null;
      },
      error: () => {
        bureau.statut = prev;
        this.togglingId = null;
      }
    });
  }

  toggleForm() {
    this.showForm = !this.showForm;
    this.success = '';
    this.error = '';
    this.form.reset();
    this.delegations = [];
  }

  submit() {
    if (this.form.invalid) return;
    this.submitting = true;
    this.adminService.createBureau(this.form.value).subscribe({
      next: () => {
        this.success = 'Bureau créé avec succès !';
        this.submitting = false;
        this.showForm = false;
        this.form.reset();
        this.delegations = [];
        this.loadBureaux();
      },
      error: (err) => {
        this.error = typeof err.error === 'string' ? err.error : 'Erreur lors de la création';
        this.submitting = false;
      }
    });
  }

  startEdit(): void {
    const b = this.selectedBureau;
    if (!b) return;

    this.editMode = true;
    this.showPassword = false;
    this.error = '';
    this.success = '';
    this.editDelegations = this.regionDelegations[b.region] || [];

    this.editForm.reset({
      email: b.email ?? '',
      matricule: b.matricule ?? '',
      region: b.region ?? '',
      delegation: b.delegation ?? '',
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
    if (!this.selectedBureau?.id) return;
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

    this.adminService.updateBureauDetails(this.selectedBureau.id, payload).subscribe({
      next: (updated) => {
        const idx = this.bureaux.findIndex((x) => x.id === this.selectedBureau?.id);
        if (idx >= 0) {
          this.bureaux[idx] = { ...this.bureaux[idx], ...updated };
        }
        this.selectedBureau = idx >= 0 ? this.bureaux[idx] : { ...this.selectedBureau, ...updated };
        this.success = 'Bureau mis à jour avec succès.';
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
    const b = this.selectedBureau;
    if (!b?.id) return;
    if (this.deleting) return;

    this.confirmTitle = 'Supprimer ce bureau ?';
    this.confirmMessage = 'Cette action supprimera aussi tous les services rattachés à ce bureau.';
    this.confirmOpen = true;
  }

  closeConfirm(): void {
    if (this.deleting) return;
    this.confirmOpen = false;
  }

  confirmDelete(): void {
    const b = this.selectedBureau;
    if (!b?.id) return;
    if (this.deleting) return;

    this.deleting = true;
    this.error = '';
    this.success = '';

    this.adminService.deleteBureau(b.id).subscribe({
      next: () => {
        this.success = 'Bureau supprimé.';
        this.deleting = false;
        this.confirmOpen = false;
        this.closeDetails();
        this.loadBureaux();
      },
      error: (err) => {
        this.error = typeof err?.error === 'string'
          ? err.error
          : (err?.error?.message ?? 'Impossible de supprimer ce bureau.');
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

    this.adminService.downloadBureauxPdf({
      region: this.filterRegion || undefined,
      delegation: this.filterDelegation || undefined
    }).subscribe({
      next: (blob) => {
        const fileName = `rapport_bureaux_${new Date().toISOString().slice(0, 16).replace(/[:T]/g, '')}.pdf`;
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
