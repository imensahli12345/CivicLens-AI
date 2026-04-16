import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { EvenementService } from '../../../../core/services/evenement.service';
import { Evenement } from '../../../../shared/models/evenement.model';
import { AuthService } from '../../../../core/services/auth.service';
import { FormsModule } from '@angular/forms'; // For ngModel if needed, but not necessary here

@Component({
  selector: 'app-evenements-feed',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './evenements-feed.component.html',
  styleUrls: ['./evenements-feed.component.css']
})
export class EvenementsFeedComponent implements OnInit {
  evenements: Evenement[] = [];
  activeTab: 'region' | 'mes' = 'mes';
  isLoading = true;
  errorMsg = '';
  participatingId: string | null = null;
  currentUserId: string | null = null;

  constructor(
    private evenementService: EvenementService,
    private authService: AuthService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUser()?.id || null;
    this.route.queryParams.subscribe(params => {
      if (params['tab'] === 'region') {
        this.activeTab = 'region';
      } else if (params['tab'] === 'mes') {
        this.activeTab = 'mes';
      }
      this.loadEvenements();
    });
  }

  setTab(tab: 'region' | 'mes'): void {
    this.activeTab = tab;
    this.loadEvenements();
  }

  loadEvenements(): void {
    this.isLoading = true;
    this.errorMsg = '';
    
    const request = this.activeTab === 'region' 
      ? this.evenementService.getRegion() 
      : this.evenementService.getMes();

    request.subscribe({
      next: (data) => {
        this.evenements = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Erreur chargement événements', err);
        this.errorMsg = 'Impossible de charger les événements.';
        this.isLoading = false;
      }
    });
  }

  isPast(dateString: any): boolean {
    return new Date(dateString) < new Date();
  }

  hasParticipated(ev: Evenement): boolean {
    if (!ev.participations || !this.currentUserId) return false;
    return ev.participations.some(p => p.citoyenId === this.currentUserId);
  }

  participer(evenement: Evenement): void {
    this.participatingId = evenement.id;
    this.evenementService.participer(evenement.id, { commentaire: "Participation depuis le feed" }).subscribe({
      next: () => {
        this.participatingId = null;
        this.errorMsg = '';
        // Mettre à jour localement ou recharger
        this.loadEvenements();
      },
      error: (err) => {
        this.participatingId = null;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible d'enregistrer votre participation pour le moment.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }
}
