import { Component, OnInit, signal, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SignalementService } from '../../../../core/services/signalement.service';
import { Signalement } from '../../../../shared/models/signalement.model';
import * as L from 'leaflet';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../core/services/auth.service';

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
  backLink = signal<string>('/dashboard/region');
  lightboxOpen = signal(false);
  lightboxUrl = signal<string | null>(null);
  voteAlert = signal(false);
  exportingPdf = signal(false);
  private voteAlertTimeoutId: ReturnType<typeof setTimeout> | null = null;
  
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
    private signalementService: SignalementService,
    private authService: AuthService
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
        const currentUserId =
          this.authService.getCurrentUserFromStorage()?.id ||
          this.authService.getCurrentUser()?.id ||
          null;
        this.backLink.set(currentUserId && data.citoyenId === currentUserId ? '/dashboard/mes-signalements' : '/dashboard/region');
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

  openLightbox(rawUrl: string, e?: Event): void {
    e?.stopPropagation();
    this.lightboxUrl.set(this.getImageUrl(rawUrl));
    this.lightboxOpen.set(true);
    document.body.style.overflow = 'hidden';
  }

  closeLightbox(e?: Event): void {
    e?.stopPropagation();
    this.lightboxOpen.set(false);
    this.lightboxUrl.set(null);
    document.body.style.overflow = '';
  }

  private isOwner(sig: Signalement): boolean {
    const userId =
      this.authService.getCurrentUserFromStorage()?.id ||
      this.authService.getCurrentUser()?.id ||
      null;
    if (!userId || !sig.citoyenId) return false;
    return String(userId).toLowerCase() === String(sig.citoyenId).toLowerCase();
  }

  private showVoteAlert(): void {
    this.voteAlert.set(true);
    if (this.voteAlertTimeoutId) clearTimeout(this.voteAlertTimeoutId);
    this.voteAlertTimeoutId = setTimeout(() => {
      this.voteAlert.set(false);
      this.voteAlertTimeoutId = null;
    }, 2200);
  }

  onVote(e?: Event): void {
    e?.stopPropagation();
    const sig = this.signalement();
    if (!sig) return;

    if (this.isOwner(sig)) {
      this.showVoteAlert();
      return;
    }
    
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

  async downloadPdf(e?: Event): Promise<void> {
    e?.stopPropagation();
    const sig = this.signalement();
    if (!sig || this.exportingPdf()) return;

    this.exportingPdf.set(true);
    this.signalementService.downloadSignalementPdf(String(sig.id)).subscribe({
      next: (blob) => {
        const fileName = `signalement_${sig.id}.pdf`;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
        this.exportingPdf.set(false);
      },
      error: () => {
        this.exportingPdf.set(false);
        this.error.set('Impossible de télécharger le PDF. Veuillez réessayer.');
      }
    });
  }


  private escapeHtml(value: string): string {
    return (value ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private buildPrintableHtml(data: {
    title: string;
    status: string;
    date: string;
    address: string;
    description: string;
    categorie: string;
    gravite: string;
    priorite: string;
    votes: number;
    coords: string;
    images: string[];
  }): string {
    const imagesHtml = data.images.length
      ? `<div class="section"><div class="h2">Images</div><div class="images">${data.images
          .map((src) => `<img src="${this.escapeHtml(src)}" alt="Image signalement" />`)
          .join('')}</div></div>`
      : '';

    return `<!doctype html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${this.escapeHtml(data.title)}</title>
  <style>
    *{box-sizing:border-box}
    body{font-family:Arial,Helvetica,sans-serif;color:#0f172a;margin:0;background:#fff}
    .page{padding:28px}
    .header{display:flex;align-items:flex-start;justify-content:space-between;gap:16px;margin-bottom:18px}
    .title{font-size:22px;font-weight:800;margin:0}
    .badge{display:inline-block;padding:6px 10px;border-radius:999px;background:#e2e8f0;font-weight:700;font-size:12px;white-space:nowrap}
    .meta{margin-top:10px;display:grid;grid-template-columns:1fr 1fr;gap:8px 14px;font-size:12px;color:#334155}
    .meta div{padding:8px 10px;border:1px solid #e2e8f0;border-radius:10px}
    .section{margin-top:14px}
    .h2{font-size:14px;font-weight:800;margin:0 0 8px 0}
    .desc{white-space:pre-wrap;line-height:1.45;font-size:13px;border:1px solid #e2e8f0;border-radius:12px;padding:12px}
    .stats{display:grid;grid-template-columns:1fr 1fr;gap:8px 14px;font-size:12px}
    .stats div{padding:8px 10px;border:1px solid #e2e8f0;border-radius:10px}
    .images{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}
    .images img{width:100%;height:auto;border-radius:12px;border:1px solid #e2e8f0;object-fit:cover}
    .footer{margin-top:18px;font-size:11px;color:#64748b}
    @media print{
      .page{padding:0}
      body{margin:16mm}
      .images{grid-template-columns:repeat(2,minmax(0,1fr))}
    }
  </style>
</head>
<body>
  <div class="page">
    <div class="header">
      <div>
        <h1 class="title">${this.escapeHtml(data.title)}</h1>
        <div class="meta">
          <div><b>Statut</b> : ${this.escapeHtml(data.status)}</div>
          <div><b>Date</b> : ${this.escapeHtml(data.date)}</div>
          <div><b>Adresse</b> : ${this.escapeHtml(data.address)}</div>
          <div><b>Coordonnées</b> : ${this.escapeHtml(data.coords)}</div>
        </div>
      </div>
      <span class="badge">CivicLens</span>
    </div>

    <div class="section">
      <div class="h2">Description</div>
      <div class="desc">${this.escapeHtml(data.description)}</div>
    </div>

    <div class="section">
      <div class="h2">Détails</div>
      <div class="stats">
        <div><b>Catégorie</b> : ${this.escapeHtml(data.categorie || '—')}</div>
        <div><b>Gravité</b> : ${this.escapeHtml(data.gravite || '—')}</div>
        <div><b>Priorité</b> : ${this.escapeHtml(data.priorite || '—')}</div>
        <div><b>Votes</b> : ${this.escapeHtml(String(data.votes ?? 0))}</div>
      </div>
    </div>

    ${imagesHtml}

    <div class="footer">Document généré depuis CivicLens AI.</div>
  </div>
  <script>
    window.addEventListener('load', () => {
      setTimeout(() => {
        try { window.focus(); } catch(e) {}
        window.print();
      }, 350);
      window.onafterprint = () => { try { window.close(); } catch(e) {} };
    });
  </script>
</body>
</html>`;
  }
}
