import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StatsCardsComponent } from '../../components/stats-cards/stats-cards.component';
import { SignalementCardComponent } from '../../components/signalement-card/signalement-card.component';
import { SignalementService } from '../../../../core/services/signalement.service';
import { EvenementService } from '../../../../core/services/evenement.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Signalement, SignalementStats } from '../../../../shared/models/signalement.model';
import { Evenement } from '../../../../shared/models/evenement.model';
import { User } from '../../../../shared/models/user.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CommonModule, RouterLink, StatsCardsComponent, SignalementCardComponent],
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.css']
})
export class HomePageComponent implements OnInit {
  user = signal<User | null>(null);
  stats = signal<SignalementStats | null>(null);
  regionEvenements = signal<Evenement[]>([]);
  delegationSignalements = signal<Signalement[]>([]);
  loadingStats = signal(true);
  loadingEvenements = signal(true);
  loadingDelegation = signal(true);

  constructor(
    private signalementService: SignalementService,
    private evenementService: EvenementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.fetchMe().subscribe({
      next: (u) => {
        this.user.set(u);
        this.loadStats();
        this.loadEvenements();
        this.loadDelegation();
      },
      error: () => {
        this.user.set(this.authService.getCurrentUser());
        this.loadStats();
        this.loadEvenements();
        this.loadDelegation();
      }
    });
  }

  loadStats(): void {
    this.signalementService.getMesStats().subscribe({
      next: (s) => { this.stats.set(s); this.loadingStats.set(false); },
      error: () => {
        this.stats.set({ total: 0, enAttente: 0, enCours: 0, resolus: 0, rejetes: 0 });
        this.loadingStats.set(false);
      }
    });
  }

  loadEvenements(): void {
    this.evenementService.getRegion().subscribe({
      next: (list) => {
        this.regionEvenements.set(list.slice(0, 3));
        this.loadingEvenements.set(false);
      },
      error: () => this.loadingEvenements.set(false)
    });
  }

  loadDelegation(): void {
    const delegation = this.user()?.delegation;
    if (!delegation) {
      this.loadingDelegation.set(false);
      return;
    }
    this.signalementService.getSignalementsDelegation(delegation).subscribe({
      next: (list) => {
        this.delegationSignalements.set(list.slice(0, 3));
        this.loadingDelegation.set(false);
      },
      error: () => this.loadingDelegation.set(false)
    });
  }

  onVote(id: string): void {
    this.signalementService.voterSignalement(id).subscribe({
      next: (updated) => {
        this.delegationSignalements.update(list =>
          list.map(s => s.id === id ? updated : s)
        );
      }
    });
  }

  goToDetails(id: string): void {
    this.router.navigate(['/dashboard/signalement', id]);
  }

  goToEventDetails(id: string): void {
    this.router.navigate(['/dashboard/evenements', id], { queryParams: { tab: 'region' } });
  }
}