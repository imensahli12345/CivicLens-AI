import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SignalementCardComponent } from '../../components/signalement-card/signalement-card.component';
import { SignalementService } from '../../../../core/services/signalement.service';
import { Signalement, SignalementCategorie, SignalementStatut } from '../../../../shared/models/signalement.model';

type FilterStatut = SignalementStatut | 'TOUS';
type FilterDomaine = SignalementCategorie | 'TOUS';

@Component({
  selector: 'app-mes-signalements',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, SignalementCardComponent],
  templateUrl: './mes-signalements.component.html',
  styleUrls: ['./mes-signalements.component.css']
})
export class MesSignalementsComponent implements OnInit {
  signalements = signal<Signalement[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  filterStatut = signal<FilterStatut>('TOUS');
  filterDomaine = signal<FilterDomaine>('TOUS');
  searchQuery = signal('');

  deleteModalOpen = signal(false);
  deleting = signal(false);
  deleteError = signal<string | null>(null);
  deleteTarget = signal<Signalement | null>(null);

  filters: { label: string; value: FilterStatut }[] = [
    { label: 'Tous',        value: 'TOUS'       },
    { label: 'En attente',  value: 'EN_ATTENTE'  },
    { label: 'En cours',    value: 'EN_COURS'    },
    { label: 'Résolus',     value: 'RESOLU'      },
    { label: 'Rejetés',     value: 'REJETE'      },
  ];

  domaines: { label: string; value: FilterDomaine }[] = [
    { label: 'Tous domaines', value: 'TOUS' },
    { label: 'Route', value: 'ROUTE' },
    { label: 'Éclairage', value: 'ECLAIRAGE' },
    { label: 'Eau', value: 'EAU' },
    { label: 'Transport', value: 'TRANSPORT' },
    { label: 'Électricité', value: 'ELECTRICITE' },
    { label: 'Infrastructure', value: 'INFRASTRUCTURE' },
    { label: 'Déchets', value: 'DECHETS' },
    { label: 'Autre', value: 'AUTRE' }
  ];

  filtered = computed(() => {
    let list = this.signalements();
    const q = this.searchQuery().toLowerCase().trim();
    const f = this.filterStatut();
    const d = this.filterDomaine();
    if (f !== 'TOUS') {
      if (f === 'EN_COURS') {
        list = list.filter(s => ['EN_COURS', 'OUVERTE', 'ENVOYEE', 'EN_COURS_DETUDE'].includes(s.statut as any));
      } else {
        list = list.filter(s => s.statut === f);
      }
    }
    if (d !== 'TOUS') {
      list = list.filter(s => s.categorie === d);
    }
    if (q) list = list.filter(s =>
      s.titre.toLowerCase().includes(q) ||
      s.adresse.toLowerCase().includes(q) ||
      s.description.toLowerCase().includes(q)
    );
    return list;
  });

  constructor(
    private signalementService: SignalementService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const statut = params['statut'];
      if (statut) {
        this.filterStatut.set(statut as FilterStatut);
      }
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.signalementService.getMesSignalements().subscribe({
      next: (list) => {
        this.signalements.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Erreur de chargement');
        this.loading.set(false);
      }
    });
  }

  setFilter(f: FilterStatut) {
    this.filterStatut.set(f);
  }

  setDomaine(d: FilterDomaine) {
    this.filterDomaine.set(d);
  }

  onSearch(event: Event) {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  onDelete(id: string): void {
    const target = this.signalements().find(s => s.id === id) || null;
    this.deleteTarget.set(target);
    this.deleteError.set(null);
    this.deleteModalOpen.set(true);
  }

  cancelDelete(): void {
    this.deleteModalOpen.set(false);
    this.deleting.set(false);
    this.deleteError.set(null);
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target || this.deleting()) return;

    this.deleting.set(true);
    this.deleteError.set(null);

    this.signalementService.deleteSignalement(target.id).subscribe({
      next: () => {
        this.signalements.update(list => list.filter(s => s.id !== target.id));
        this.cancelDelete();
      },
      error: (err) => {
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : (err?.message || 'Erreur lors de la suppression');
        this.deleteError.set(msg);
        this.deleting.set(false);
      }
    });
  }
}
