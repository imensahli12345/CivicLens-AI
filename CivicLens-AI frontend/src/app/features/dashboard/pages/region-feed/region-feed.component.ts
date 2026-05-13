import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalementCardComponent } from '../../components/signalement-card/signalement-card.component';
import { SignalementService } from '../../../../core/services/signalement.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Router, RouterLink } from '@angular/router';
import { Signalement, SignalementCategorie, SignalementStatut } from '../../../../shared/models/signalement.model';

type SortOption = 'recent' | 'votes';
type FilterValue = 'TOUTES' | string;
type FilterStatut = SignalementStatut | 'TOUS' | 'EN_COURS';

@Component({
  selector: 'app-region-feed',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SignalementCardComponent],
  templateUrl: './region-feed.component.html',
  styleUrls: ['./region-feed.component.css']
})
export class RegionFeedComponent implements OnInit {
  signalements = signal<Signalement[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  sortBy = signal<SortOption>('recent');
  filterCat = signal<SignalementCategorie | 'TOUTES'>('TOUTES');
  filterGravite = signal<FilterValue>('TOUTES');
  filterPriorite = signal<FilterValue>('TOUTES');
  filterStatut = signal<FilterStatut>('TOUS');
  userDelegation = signal<string>('');
  private currentUserId = signal<string | null>(null);

  categories: { value: SignalementCategorie | 'TOUTES'; label: string }[] = [
    { value: 'TOUTES',       label: 'Toutes catégories' },
    { value: 'ROUTE',          label: '🛣️ Route' },
    { value: 'ECLAIRAGE',      label: '💡 Éclairage' },
    { value: 'EAU',            label: '💧 Eau' },
    { value: 'ELECTRICITE',    label: '⚡ Électricité' },
    { value: 'INFRASTRUCTURE', label: '🏗️ Infrastructure' },
    { value: 'DECHETS',        label: '🗑️ Déchets' },
    { value: 'AUTRE',          label: '📌 Autre' },
  ];

  statutOptions: { value: FilterStatut; label: string }[] = [
    { value: 'TOUS', label: 'Tous statuts' },
    { value: 'EN_ATTENTE', label: 'En attente' },
    { value: 'EN_COURS', label: 'En cours' },
    { value: 'RESOLU', label: 'Résolu' },
    { value: 'REJETE', label: 'Rejeté' },
  ];

  graviteOptions: { value: FilterValue; label: string }[] = [
    { value: 'TOUTES', label: 'Toutes gravités' },
    { value: 'NON_DEFINIE', label: 'Non définie' },
    { value: 'FAIBLE', label: 'FAIBLE' },
    { value: 'MOYEN', label: 'MOYEN' },
    { value: 'ELEVE', label: 'ELEVE' },
    { value: 'CRITIQUE', label: 'CRITIQUE' },
  ];

  prioriteOptions: { value: FilterValue; label: string }[] = [
    { value: 'TOUTES', label: 'Toutes priorités' },
    { value: 'NON_DEFINIE', label: 'Non définie' },
    { value: 'BASSE', label: 'BASSE' },
    { value: 'MOYENNE', label: 'MOYENNE' },
    { value: 'HAUTE', label: 'HAUTE' },
    { value: 'URGENTE', label: 'URGENTE' },
  ];

  filtered = computed(() => {
    let list = [...this.signalements()];
    const cat = this.filterCat();
    if (cat !== 'TOUTES') list = list.filter(s => s.categorie === cat);
    const st = this.filterStatut();
    if (st !== 'TOUS') {
      if (st === 'EN_COURS') {
        list = list.filter(s => ['EN_COURS', 'OUVERTE', 'ENVOYEE', 'EN_COURS_DETUDE'].includes(s.statut as any));
      } else {
        list = list.filter(s => s.statut === st);
      }
    }
    const g = this.filterGravite();
    if (g !== 'TOUTES') {
      if (g === 'NON_DEFINIE') list = list.filter(s => !s.gravite || !String(s.gravite).trim());
      else list = list.filter(s => (s.gravite || '').toString() === g);
    }
    const p = this.filterPriorite();
    if (p !== 'TOUTES') {
      if (p === 'NON_DEFINIE') list = list.filter(s => !s.priorite || !String(s.priorite).trim());
      else list = list.filter(s => (s.priorite || '').toString() === p);
    }
    if (this.sortBy() === 'votes') {
      list.sort((a, b) => b.votes - a.votes);
    } else {
      list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
    }
    return list;
  });

  constructor(
    private signalementService: SignalementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const cachedUser = this.authService.getCurrentUserFromStorage() || this.authService.getCurrentUser();
    if (cachedUser?.id) this.currentUserId.set(cachedUser.id);
    if (cachedUser?.delegation) {
      this.userDelegation.set(cachedUser.delegation);
      this.fetchSignalements(cachedUser.delegation);
    }
    this.authService.fetchMe().subscribe({
      next: (user) => {
        const delegation = user.delegation;
        if (!delegation) {
          this.error.set('Délégation de l\'utilisateur introuvable');
          this.loading.set(false);
          return;
        }
        this.currentUserId.set(user.id);
        this.userDelegation.set(delegation);
        this.fetchSignalements(delegation);
      },
      error: () => {
        const fallbackUser = this.authService.getCurrentUserFromStorage() || this.authService.getCurrentUser();
        if (!fallbackUser?.delegation) {
          this.error.set('Délégation de l\'utilisateur introuvable (hors ligne)');
          this.loading.set(false);
          return;
        }
        if (fallbackUser?.id) this.currentUserId.set(fallbackUser.id);
        this.userDelegation.set(fallbackUser.delegation);
        this.fetchSignalements(fallbackUser.delegation);
      }
    });
  }

  private fetchSignalements(delegation: string): void {
    this.signalementService.getSignalementsDelegation(delegation).subscribe({
      next: (list) => {
        const myId = this.currentUserId();
        this.signalements.set(myId ? list.filter(s => s.citoyenId !== myId) : list);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'Erreur de chargement');
        this.loading.set(false);
      }
    });
  }

  onVote(id: string): void {
    this.signalementService.voterSignalement(id).subscribe({
      next: () => {
        this.signalements.update(list => list.map(s => {
          if (s.id === id) {
            // Optimistic update of votes count and aVote boolean
            return { ...s, votes: (s.votes || 0) + 1, aVote: true };
          }
          return s;
        }));
      }
    });
  }

  goToDetails(id: string): void {
    this.router.navigate(['/dashboard/signalement', id]);
  }
}
