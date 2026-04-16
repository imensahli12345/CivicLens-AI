import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EvenementService } from '../../../../core/services/evenement.service';
import { Evenement } from '../../../../shared/models/evenement.model';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-evenement-details',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './evenement-details.component.html',
  styleUrls: ['./evenement-details.component.css']
})
export class EvenementDetailsComponent implements OnInit {
  evenement: Evenement | null = null;
  isLoading = true;
  errorMsg = '';
  isParticipating = false;
  currentUserId: string | null = null;
  backTab: 'region' | 'mes' = 'region';

  editModalOpen = false;
  deleteModalOpen = false;
  cancelModalOpen = false;
  confirmModalOpen = false;
  saving = false;
  deleting = false;
  canceling = false;
  confirming = false;
  editLieu = '';
  editDateLocal = '';

  constructor(
    private route: ActivatedRoute,
    private evenementService: EvenementService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.authService.getCurrentUser()?.id || null;
    
    this.route.queryParamMap.subscribe(q => {
      const tab = q.get('tab');
      this.backTab = tab === 'mes' ? 'mes' : 'region';
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadDetails(id);
      } else {
        this.errorMsg = "Identifiant d'événement manquant.";
        this.isLoading = false;
      }
    });
  }

  loadDetails(id: string): void {
    this.isLoading = true;
    this.evenementService.getById(id).subscribe({
      next: (data) => {
        this.evenement = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMsg = "Impossible de charger les détails de l'événement.";
        this.isLoading = false;
      }
    });
  }

  get isOrganizer(): boolean {
    if (!this.evenement || !this.currentUserId) return false;
    return this.evenement.organisateurId === this.currentUserId;
  }

  get canManageEvent(): boolean {
    if (!this.evenement || !this.isOrganizer) return false;
    const evDate = new Date(this.evenement.dateEvenement).getTime();
    const now = Date.now();
    const diffHours = (evDate - now) / (1000 * 3600);
    return diffHours >= 24;
  }

  get timeLockHint(): string | null {
    if (!this.evenement || !this.isOrganizer) return null;
    const evDate = new Date(this.evenement.dateEvenement).getTime();
    const now = Date.now();
    const diffHours = (evDate - now) / (1000 * 3600);
    if (diffHours < 24) return "Modification et suppression bloquées à moins de 24 heures de l'événement.";
    return null;
  }

  get canConfirmWindow(): boolean {
    if (!this.evenement || !this.isOrganizer) return false;
    if (this.evenement.isConfirmed) return false;
    const evDate = new Date(this.evenement.dateEvenement).getTime();
    const now = Date.now();
    const diffHours = (evDate - now) / (1000 * 3600);
    return diffHours <= 20 && diffHours >= 10;
  }

  get confirmHint(): string | null {
    if (!this.evenement || !this.isOrganizer) return null;
    if (this.evenement.isConfirmed) return "Événement déjà confirmé.";
    const evDate = new Date(this.evenement.dateEvenement).getTime();
    const now = Date.now();
    const diffHours = (evDate - now) / (1000 * 3600);
    if (diffHours > 20) return "La confirmation est possible au plus tôt 20 heures avant l'événement.";
    if (diffHours < 10) return "La confirmation est impossible à moins de 10 heures de l'événement.";
    return null;
  }

  get hasParticipated(): boolean {
    if (!this.evenement || !this.evenement.participations || !this.currentUserId) return false;
    return this.evenement.participations.some(p => p.citoyenId === this.currentUserId);
  }

  get canCancel(): boolean {
    if (!this.evenement || !this.hasParticipated) return false;
    const evDate = new Date(this.evenement.dateEvenement).getTime();
    const now = new Date().getTime();
    const diffDays = (evDate - now) / (1000 * 3600 * 24);
    return diffDays >= 3;
  }

  participer(): void {
    if (!this.evenement) return;
    this.isParticipating = true;
    this.evenementService.participer(this.evenement.id, { commentaire: "Je participe !" }).subscribe({
      next: () => {
        this.isParticipating = false;
        this.errorMsg = '';
        this.loadDetails(this.evenement!.id); // recharger pour voir ma participation
      },
      error: (err) => {
        this.isParticipating = false;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible d'enregistrer votre participation pour le moment.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  openEdit(): void {
    if (!this.evenement) return;
    this.errorMsg = '';
    this.editLieu = this.evenement.lieu || '';
    const d = new Date(this.evenement.dateEvenement);
    // datetime-local expects YYYY-MM-DDTHH:mm in local time
    const pad = (n: number) => String(n).padStart(2, '0');
    this.editDateLocal = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
    this.editModalOpen = true;
  }

  closeEdit(): void {
    this.editModalOpen = false;
    this.saving = false;
  }

  saveEdit(): void {
    if (!this.evenement || this.saving) return;
    if (!this.editLieu.trim() || !this.editDateLocal) {
      this.errorMsg = 'Veuillez renseigner le lieu et la date.';
      window.scrollTo({ top: 0, behavior: 'smooth' });
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    const iso = new Date(this.editDateLocal).toISOString();
    this.evenementService.update(this.evenement.id, { lieu: this.editLieu.trim(), dateEvenement: iso }).subscribe({
      next: () => {
        this.saving = false;
        this.closeEdit();
        this.loadDetails(this.evenement!.id);
      },
      error: (err) => {
        this.saving = false;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible de modifier l'événement.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  openDelete(): void {
    this.errorMsg = '';
    this.deleteModalOpen = true;
  }

  closeDelete(): void {
    this.deleteModalOpen = false;
    this.deleting = false;
  }

  confirmDelete(): void {
    if (!this.evenement || this.deleting) return;
    this.deleting = true;
    this.errorMsg = '';
    this.evenementService.deleteEvenement(this.evenement.id).subscribe({
      next: () => {
        // back to list (same tab)
        this.router.navigate(['/dashboard/evenements'], { queryParams: { tab: this.backTab } });
      },
      error: (err) => {
        this.deleting = false;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible de supprimer l'événement.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  openCancelParticipation(): void {
    this.errorMsg = '';
    this.cancelModalOpen = true;
  }

  closeCancelParticipation(): void {
    this.cancelModalOpen = false;
    this.canceling = false;
  }

  confirmCancelParticipation(): void {
    if (!this.evenement || this.canceling) return;
    this.canceling = true;
    this.errorMsg = '';

    this.evenementService.annulerParticipation(this.evenement.id).subscribe({
      next: () => {
        this.canceling = false;
        this.closeCancelParticipation();
        this.loadDetails(this.evenement!.id);
      },
      error: (err) => {
        this.canceling = false;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible d'annuler votre participation.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  openConfirmEvenement(): void {
    this.errorMsg = '';
    this.confirmModalOpen = true;
  }

  closeConfirmEvenement(): void {
    this.confirmModalOpen = false;
    this.confirming = false;
  }

  confirmEvenement(): void {
    if (!this.evenement || this.confirming) return;
    this.confirming = true;
    this.errorMsg = '';

    this.evenementService.confirmEvenement(this.evenement.id).subscribe({
      next: () => {
        this.confirming = false;
        this.closeConfirmEvenement();
        this.loadDetails(this.evenement!.id);
      },
      error: (err) => {
        this.confirming = false;
        const msg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible de confirmer l'événement.";
        this.errorMsg = msg;
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    });
  }

  annulerParticipation(): void {
    // Kept for compatibility: prefer modal flow
    this.openCancelParticipation();
  }

  isPast(dateString: any): boolean {
    return new Date(dateString) < new Date();
  }
}
