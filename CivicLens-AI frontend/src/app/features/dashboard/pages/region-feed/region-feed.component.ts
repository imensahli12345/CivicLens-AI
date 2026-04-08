import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalementCardComponent } from '../../components/signalement-card/signalement-card.component';
import { SignalementService } from '../../../../core/services/signalement.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Router, RouterLink } from '@angular/router';
import { Signalement, SignalementCategorie } from '../../../../shared/models/signalement.model';

type SortOption = 'recent' | 'votes';

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
  userDelegation = signal<string>('');
  private currentUserId = signal<string | null>(null);

  categories: { value: SignalementCategorie | 'TOUTES'; label: string }[] = [
    { value: 'TOUTES',       label: 'Toutes catégories' },
    { value: 'ROUTE',          label: '🛣️ Route' },
    { value: 'ECLAIRAGE',      label: '💡 Éclairage' },
    { value: 'EAU',            label: '💧 Eau' },
    { value: 'TRANSPORT',      label: '🚌 Transport' },
    { value: 'ELECTRICITE',    label: '⚡ Électricité' },
    { value: 'INFRASTRUCTURE', label: '🏗️ Infrastructure' },
    { value: 'DECHETS',        label: '🗑️ Déchets' },
    { value: 'AUTRE',          label: '📌 Autre' },
  ];

  filtered = computed(() => {
    let list = [...this.signalements()];
    const cat = this.filterCat();
    if (cat !== 'TOUTES') list = list.filter(s => s.categorie === cat);
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
