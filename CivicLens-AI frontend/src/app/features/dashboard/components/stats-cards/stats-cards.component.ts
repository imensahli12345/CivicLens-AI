import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { SignalementStats } from '../../../../shared/models/signalement.model';

@Component({
  selector: 'app-stats-cards',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stats-cards.component.html',
  styleUrls: ['./stats-cards.component.css']
})
export class StatsCardsComponent {
  @Input() stats: SignalementStats | null = null;
  @Input() loading = false;

  cards = [
    { key: 'total',     label: 'Total',       icon: 'layers',   color: 'accent',  statusFilter: 'TOUS' },
    { key: 'enAttente', label: 'En attente',  icon: 'clock',    color: 'warning', statusFilter: 'EN_ATTENTE' },
    { key: 'enCours',   label: 'En cours',    icon: 'activity', color: 'info',    statusFilter: 'EN_COURS' },
    { key: 'resolus',   label: 'Résolus',     icon: 'check',    color: 'success', statusFilter: 'RESOLU' },
  ];

  constructor(private router: Router) {}

  getValue(key: string): number {
    if (!this.stats) return 0;
    return (this.stats as any)[key] ?? 0;
  }

  goToStatus(status: string) {
    this.router.navigate(['/dashboard/mes-signalements'], { queryParams: { statut: status } });
  }
}
