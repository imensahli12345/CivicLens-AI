import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EvenementService } from '../../../../core/services/evenement.service';
import { Evenement } from '../../../../shared/models/evenement.model';

@Component({
  selector: 'app-evenement-participants',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './evenement-participants.component.html',
  styleUrls: ['./evenement-participants.component.css']
})
export class EvenementParticipantsComponent implements OnInit {
  evenement: Evenement | null = null;
  isLoading = true;
  errorMsg = '';
  backTab: 'region' | 'mes' = 'region';

  constructor(
    private route: ActivatedRoute,
    private evenementService: EvenementService
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(q => {
      const tab = q.get('tab');
      this.backTab = tab === 'mes' ? 'mes' : 'region';
    });

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (!id) {
        this.errorMsg = "Identifiant d'événement manquant.";
        this.isLoading = false;
        return;
      }
      this.load(id);
    });
  }

  private load(id: string): void {
    this.isLoading = true;
    this.errorMsg = '';
    this.evenementService.getById(id).subscribe({
      next: (data) => {
        this.evenement = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMsg =
          (err?.error && typeof err.error === 'string' && err.error.trim())
            ? err.error
            : "Impossible de charger la liste des participants.";
        this.isLoading = false;
      }
    });
  }
}

