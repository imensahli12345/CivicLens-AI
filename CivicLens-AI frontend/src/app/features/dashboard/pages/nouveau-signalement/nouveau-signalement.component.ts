import { Component, signal, AfterViewInit, HostListener, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SignalementService } from '../../../../core/services/signalement.service';
import { SignalementCategorie } from '../../../../shared/models/signalement.model';

import * as L from 'leaflet';

@Component({
  selector: 'app-nouveau-signalement',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './nouveau-signalement.component.html',
  styleUrls: ['./nouveau-signalement.component.css']
})
export class NouveauSignalementComponent implements AfterViewInit, OnDestroy {
  form: FormGroup;
  loading = signal(false);
  success = signal(false);
  error = signal<string | null>(null);
  errorLink = signal<string | null>(null);
  
  images: Array<{ file: File; url: string; name: string }> = [];
  locating = signal(false);
  addressSearching = signal(false);
  previewOpen = signal(false);
  previewIndex = signal(0);
  
  private map: L.Map | undefined;
  private marker: L.Marker | undefined;
  private defaultIcon = L.icon({
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    iconSize: [25, 41],
    iconAnchor: [12, 41]
  });

  constructor(
    private fb: FormBuilder,
    private signalementService: SignalementService,
    private router: Router
  ) {
    this.form = this.fb.group({
      description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(500)]],
      adresseTexte: ['', [Validators.maxLength(180)]],
      latitude:    ['', Validators.required],
      longitude:   ['', Validators.required],
    });
  }

  ngAfterViewInit(): void {
    if (typeof window !== 'undefined') {
      this.initMap();
    }
  }

  ngOnDestroy(): void {
    for (const img of this.images) {
      if (img?.url) URL.revokeObjectURL(img.url);
    }
    if (this.map) {
      this.map.off();
      this.map.remove();
      this.map = undefined;
    }
    document.body.style.overflow = '';
  }

  private initMap(): void {
    // Default coords (e.g. Tunis center)
    this.map = L.map('map').setView([36.8065, 10.1815], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap'
    }).addTo(this.map);

    this.map.on('click', (e: L.LeafletMouseEvent) => {
      this.setMapPin(e.latlng.lat, e.latlng.lng);
    });
  }

  private setMapPin(lat: number, lng: number): void {
    if (this.map) {
      if (this.marker) {
        this.map.removeLayer(this.marker);
      }
      this.marker = L.marker([lat, lng], { icon: this.defaultIcon }).addTo(this.map);
      this.form.patchValue({
        latitude: lat,
        longitude: lng
      });
      this.form.get('latitude')?.markAsTouched();
      this.form.get('longitude')?.markAsTouched();
    }
  }

  get f() { return this.form.controls; }

  onFileChange(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      const files = Array.from(event.target.files) as File[];
      
      const validFiles = files.filter(f => f.type.startsWith('image/'));
      
      if (this.images.length + validFiles.length > 3) {
        this.error.set('Vous pouvez ajouter un maximum de 3 images.');
        this.errorLink.set(null);
        return;
      }
      
      const appended = validFiles.map((file) => ({
        file,
        url: URL.createObjectURL(file),
        name: file.name || 'image'
      }));
      this.images = [...this.images, ...appended];
      this.error.set(null);
      this.errorLink.set(null);
      event.target.value = '';
    }
  }

  removeImage(index: number): void {
    const item = this.images[index];
    if (item?.url) URL.revokeObjectURL(item.url);
    this.images.splice(index, 1);
    if (this.previewOpen()) {
      if (this.images.length === 0) {
        this.closePreview();
        return;
      }
      const nextIndex = Math.min(this.previewIndex(), this.images.length - 1);
      this.previewIndex.set(nextIndex);
    }
  }

  openPreview(index: number): void {
    if (!this.images[index]) return;
    this.previewIndex.set(index);
    this.previewOpen.set(true);
    document.body.style.overflow = 'hidden';
  }

  closePreview(event?: Event): void {
    event?.stopPropagation();
    this.previewOpen.set(false);
    document.body.style.overflow = '';
  }

  prevPreview(event?: Event): void {
    event?.stopPropagation();
    if (this.images.length === 0) return;
    const idx = this.previewIndex();
    const next = (idx - 1 + this.images.length) % this.images.length;
    this.previewIndex.set(next);
  }

  nextPreview(event?: Event): void {
    event?.stopPropagation();
    if (this.images.length === 0) return;
    const idx = this.previewIndex();
    const next = (idx + 1) % this.images.length;
    this.previewIndex.set(next);
  }

  get previewUrl(): string | null {
    return this.images[this.previewIndex()]?.url ?? null;
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.previewOpen()) return;
    if (event.key === 'Escape') this.closePreview();
    if (event.key === 'ArrowLeft') this.prevPreview();
    if (event.key === 'ArrowRight') this.nextPreview();
  }

  locateMe(): void {
    if (navigator.geolocation) {
      this.locating.set(true);
      this.error.set(null);
      this.errorLink.set(null);
      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.setMapPin(position.coords.latitude, position.coords.longitude);
          if (this.map) {
            this.map.setView([position.coords.latitude, position.coords.longitude], 16);
          }
          this.locating.set(false);
        },
        (error) => {
          this.locating.set(false);
          this.error.set('Impossible d\'obtenir votre position. Veuillez autoriser la localisation.');
          this.errorLink.set(null);
        }
      );
    } else {
      this.error.set('La géolocalisation n\'est pas supportée par votre navigateur.');
      this.errorLink.set(null);
    }
  }

  async locateAddress(): Promise<void> {
    if (this.addressSearching()) return;

    const raw = String(this.form.get('adresseTexte')?.value ?? '').trim();
    if (!raw) {
      this.error.set('Veuillez saisir une adresse puis cliquer sur "Localiser l’adresse".');
      this.errorLink.set(null);
      return;
    }

    this.addressSearching.set(true);
    this.error.set(null);
    this.errorLink.set(null);

    try {
      const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&q=${encodeURIComponent(raw)}&limit=1`;
      const resp = await fetch(url, { headers: { Accept: 'application/json' } });
      if (!resp.ok) throw new Error('Recherche impossible.');
      const data = await resp.json();
      const first = Array.isArray(data) ? data[0] : null;
      const lat = first?.lat ? Number(first.lat) : NaN;
      const lon = first?.lon ? Number(first.lon) : NaN;
      if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
        this.error.set('Adresse introuvable. Essayez de préciser (ville, rue, numéro).');
        this.errorLink.set(null);
        return;
      }

      this.setMapPin(lat, lon);
      if (this.map) this.map.setView([lat, lon], 16);
    } catch {
      this.error.set('Erreur lors de la recherche d’adresse. Réessayez ou utilisez la carte.');
      this.errorLink.set(null);
    } finally {
      this.addressSearching.set(false);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (!this.form.value.latitude || !this.form.value.longitude) {
        this.error.set('Veuillez vous localiser (Me localiser, carte, ou adresse) avant de soumettre.');
      }
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.errorLink.set(null);

    const formData = {
      ...this.form.value,
      images: this.images.map(i => i.file)
    };

    this.signalementService.createSignalement(formData).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/dashboard/mes-signalements']), 1800);
      },
      error: (err) => {
        this.loading.set(false);
        const directLink = typeof (err as any)?.link === 'string' ? String((err as any).link) : '';
        const parsed = this.parseMessageWithLink(err?.message || '');
        this.error.set(parsed.text || 'Erreur lors de la création du signalement');
        this.errorLink.set((directLink && directLink.trim()) ? directLink.trim() : parsed.link);
      }
    });
  }

  private parseMessageWithLink(raw: string): { text: string; link: string | null } {
    const value = (raw ?? '').trim();
    if (!value) return { text: '', link: null };

    const match = value.match(/https?:\/\/[^\s]+|\/dashboard\/signalement\/\d+/);
    if (!match) return { text: value, link: null };

    const matched = match[0];
    const cleanedUrl = matched.replace(/[)\].,;!?]+$/g, '');
    const link = cleanedUrl;

    let text = value.replace(matched, '');
    if (matched !== cleanedUrl) {
      text = text.replace(cleanedUrl, '');
    }
    text = text.replace(/\s{2,}/g, ' ').trim();
    text = text.replace(/[:：]\s*$/g, '').trim();

    return { text, link };
  }

  hasError(field: string, error: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.touched && ctrl?.hasError(error));
  }
}
