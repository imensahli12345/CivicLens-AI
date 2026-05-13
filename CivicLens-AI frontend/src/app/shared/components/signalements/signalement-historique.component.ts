import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { HistoriqueEntry } from '../../../core/services/bureau-signalements.service';

@Component({
  selector: 'app-signalement-historique',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './signalement-historique.component.html',
  styleUrls: ['./signalement-historique.component.css']
})
export class SignalementHistoriqueComponent {
  @Input() open = false;
  @Input() title = 'Historique du signalement';
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() entries: HistoriqueEntry[] = [];

  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  formatDate(value: string): string {
    if (!value) return '-';
    return new Date(value).toLocaleString('fr-FR', {
      year: 'numeric',
      month: 'short',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
