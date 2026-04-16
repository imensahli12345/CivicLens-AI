import { Component, signal, AfterViewInit } from '@angular/core';
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
export class NouveauSignalementComponent implements AfterViewInit {
  form: FormGroup;
  loading = signal(false);
  success = signal(false);
  error = signal<string | null>(null);
  
  images: File[] = [];
  locating = signal(false);
  
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
      latitude:    ['', Validators.required],
      longitude:   ['', Validators.required],
    });
  }

  ngAfterViewInit(): void {
    if (typeof window !== 'undefined') {
      this.initMap();
    }
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
        return;
      }
      
      this.images = [...this.images, ...validFiles];
      this.error.set(null);
      event.target.value = '';
    }
  }

  removeImage(index: number): void {
    this.images.splice(index, 1);
  }

  locateMe(): void {
    if (navigator.geolocation) {
      this.locating.set(true);
      this.error.set(null);
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
        }
      );
    } else {
      this.error.set('La géolocalisation n\'est pas supportée par votre navigateur.');
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (!this.form.value.latitude || !this.form.value.longitude) {
        this.error.set('Veuillez vous localiser avant de soumettre.');
      }
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const formData = {
      ...this.form.value,
      images: this.images
    };

    this.signalementService.createSignalement(formData).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/dashboard/mes-signalements']), 1800);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message || 'Erreur lors de la création du signalement');
      }
    });
  }

  hasError(field: string, error: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.touched && ctrl?.hasError(error));
  }
}