import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Signalement } from '../../../../shared/models/signalement.model';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-signalement-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './signalement-card.component.html',
  styleUrls: ['./signalement-card.component.css']
})
export class SignalementCardComponent {
  @Input() signalement!: Signalement;
  @Input() showActions = false;
  @Input() showVote = false;
  @Input() showDetailsBtn = false;

  @Output() onDelete = new EventEmitter<string>();
  @Output() onVote   = new EventEmitter<string>();

  statusLabels: Record<string, string> = {
    EN_ATTENTE: 'En attente',
    EN_COURS:   'En cours',
    OUVERTE:    'En cours',
    ENVOYEE:    'En cours',
    EN_COURS_DETUDE: 'En cours',
    RESOLU:     'Résolu',
    REJETE:     'Rejeté'
  };

  statusClasses: Record<string, string> = {
    EN_ATTENTE: 'badge-waiting',
    EN_COURS:   'badge-progress',
    OUVERTE:    'badge-progress',
    ENVOYEE:    'badge-progress',
    EN_COURS_DETUDE: 'badge-progress',
    RESOLU:     'badge-resolved',
    REJETE:     'badge-rejected'
  };

  categorieLabels: Record<string, string> = {
    VOIRIE:        '🛣️ Voirie',
    ECLAIRAGE:     '💡 Éclairage',
    PROPRETE:      '🗑️ Propreté',
    ESPACES_VERTS: '🌳 Espaces verts',
    EAU:           '💧 Eau',
    TRANSPORT:     '🚌 Transport',
    SECURITE:      '🔒 Sécurité',
    AUTRE:         '📌 Autre'
  };

  getStatusLabel()  { return this.statusLabels[this.signalement.statut]   || this.signalement.statut; }
  getStatusClass()  { return this.statusClasses[this.signalement.statut]  || ''; }
  getCategorieLabel(){ return this.categorieLabels[this.signalement.categorie] || this.signalement.categorie; }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('fr-TN', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  delete(e: Event) {
    e.stopPropagation();
    this.onDelete.emit(this.signalement.id);
  }

  vote(e: Event) {
    e.stopPropagation();
    this.onVote.emit(this.signalement.id);
  }

  getImageUrl(): string | null {
    if (!this.signalement.imageUrl) return null;
    const baseUrl = environment.apiUrl.replace('/api', '');
    const url = this.signalement.imageUrl;
    if (url.startsWith('http') || url.startsWith('data:')) return url;
    if (url.startsWith('/')) return `${baseUrl}${url}`;
    return `${baseUrl}/${url}`;
  }

  getFormattedAddress(): string {
    if (!this.signalement.adresse) return 'Localisation inconnue';
    try {
      const addr = JSON.parse(this.signalement.adresse);
      const city = addr.city || addr.town || addr.village || addr.county || '';
      const state = addr.state || '';
      if (city && state) return `${city}, ${state}`;
      return city || state || 'Localisation inconnue';
    } catch {
      return this.signalement.adresse;
    }
  }
}
