import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface QuickActionItem {
  id: string;
  title: string;
  subtitle: string;
  icon: string;
  accentColor: string;
}

@Component({
  selector: 'app-quick-actions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './quick-actions.component.html',
  styleUrls: ['./quick-actions.component.css']
})
export class QuickActionsComponent {
  @Input() heading = 'Actions rapides';
  @Input() actions: QuickActionItem[] = [
    {
      id: 'filter-status',
      title: 'Filtrer par statut',
      subtitle: 'Afficher les signalements selon leur statut',
      icon: 'layers',
      accentColor: '#a78bfa'
    },
    {
      id: 'filter-gravite',
      title: 'Filtrer par gravite',
      subtitle: 'Retrouver les signalements selon leur gravite',
      icon: 'alert',
      accentColor: '#5eead4'
    },
    {
      id: 'filter-priorite',
      title: 'Filtrer par priorite',
      subtitle: 'Consulter les signalements par niveau de priorite',
      icon: 'flag',
      accentColor: '#93c5fd'
    },
    {
      id: 'filter-date',
      title: 'Filtrer par date',
      subtitle: 'Choisir une plage de resolution pour afficher les resultats',
      icon: 'calendar-range',
      accentColor: '#86efac'
    }
  ];

  @Output() actionSelected = new EventEmitter<QuickActionItem>();

  onActionClick(action: QuickActionItem): void {
    this.actionSelected.emit(action);
  }
}
