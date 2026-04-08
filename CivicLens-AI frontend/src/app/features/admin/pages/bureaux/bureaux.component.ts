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
  delegations: string[] = [];
  filterRegion = '';
  filterDelegation = '';
  filterDelegations: string[] = [];
  selectedBureau: any | null = null;
  detailsOpen = false;
  togglingId: string | null = null;

  form: FormGroup;

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
  }

  closeDetails(): void {
    this.detailsOpen = false;
    this.selectedBureau = null;
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
}
