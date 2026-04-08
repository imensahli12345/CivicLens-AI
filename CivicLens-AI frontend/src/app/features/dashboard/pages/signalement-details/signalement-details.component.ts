import { Component, OnInit, signal, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SignalementService } from '../../../../core/services/signalement.service';
import { Signalement } from '../../../../shared/models/signalement.model';
import * as L from 'leaflet';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-signalement-details',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './signalement-details.component.html',
  styleUrl: './signalement-details.component.css'
})
export class SignalementDetailsComponent implements OnInit, AfterViewInit {
  signalement = signal<Signalement | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  
  private map: L.Map | undefined;
  // Use custom marker to avoid webpack issues
  private defaultIcon = L.icon({
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    iconSize: [25, 41],
    iconAnchor: [12, 41]
  });

  statusLabels: Record<string, string> = {
    EN_ATTENTE: 'En attente',
    EN_COURS:   'En cours',
    RESOLU:     'Résolu',
    REJETE:     'Rejeté'
  };

  statusClasses: Record<string, string> = {
    EN_ATTENTE: 'badge-waiting',
    EN_COURS:   'badge-progress',
    RESOLU:     'badge-resolved',
    REJETE:     'badge-rejected'
  };

  constructor(
    private route: ActivatedRoute,
    private signalementService: SignalementService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadSignalement(id);
    } else {
      this.error.set("Identifiant introuvable.");
      this.loading.set(false);
    }
  }

  loadSignalement(id: string): void {
    this.signalementService.getSignalementById(id).subscribe({
      next: (data) => {
        this.signalement.set(data);
        this.loading.set(false);
        // Map will be drawn in AfterViewInit or when data loads if AfterViewInit already ran
        setTimeout(() => this.initMap(data.latitude || 0, data.longitude || 0), 100);
      },
      error: (err) => {
        this.error.set(err.message || "Impossible de charger le signalement.");
        this.loading.set(false);
      }
    });
  }

  ngAfterViewInit(): void {
    // Map is initialized once data is loaded
  }

  private initMap(lat: number, lng: number): void {
    if (typeof window === 'undefined') return;
    
    // Only init map if not already initialized
    if (!this.map && document.getElementById('details-map')) {
      this.map = L.map('details-map', {
        zoomControl: false,
        dragging: false,
        scrollWheelZoom: false,
        doubleClickZoom: false
      }).setView([lat, lng], 15);

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap'
      }).addTo(this.map);

      L.marker([lat, lng], { icon: this.defaultIcon }).addTo(this.map);
    }
  }

  getFormattedAddress(adresse: string | undefined): string {
    if (!adresse) return 'Localisation inconnue';
    try {
      const addr = JSON.parse(adresse);
      const city = addr.city || addr.town || addr.village || addr.county || '';
      const state = addr.state || '';
      if (city && state) return `${city}, ${state}`;
      return city || state || 'Localisation inconnue';
    } catch {
      return adresse;
    }
  }

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  }

  getImageUrl(url: string): string {
    const baseUrl = environment.apiUrl.replace('/api', '');
    if (url.startsWith('http') || url.startsWith('data:')) return url;
    if (url.startsWith('/')) return `${baseUrl}${url}`;
    return `${baseUrl}/${url}`;
  }

  onVote(): void {
    const sig = this.signalement();
    if (!sig) return;
    
    this.signalementService.voterSignalement(sig.id).subscribe({
      next: () => {
        this.signalement.set({
          ...sig,
          votes: (sig.votes || 0) + 1,
          aVote: true
        });
      },
      error: (err) => {
        console.error('Erreur lors du vote', err);
      }
    });
  }
}
