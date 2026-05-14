import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ActorDashboardTopbarComponent } from '../../../../shared/components/actor-dashboard-topbar/actor-dashboard-topbar.component';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import * as L from 'leaflet';
import { MapReportDetail, MapReportMarker, MapSignalementsService } from '../../../../core/services/map-signalements.service';

@Component({
  selector: 'app-organisation-map',
  standalone: true,
  imports: [CommonModule, ActorDashboardTopbarComponent, ReactiveFormsModule],
  templateUrl: './organisation-map.component.html',
  styleUrl: './organisation-map.component.css'
})
export class OrganisationMapComponent implements OnInit, AfterViewInit, OnDestroy {
  userName = 'CivicLens Organisation';
  loading = true;
  isLoadingMarkers = false;
  error: string | null = null;

  markerSummary = { total: 0, returned: 0 };
  selectedDetail: MapReportDetail | null = null;
  private currentReports: MapReportMarker[] = [];

  readonly statutOptions = [
    { value: '', label: 'Tous les statuts' },
    { value: 'EN_ATTENTE', label: 'En attente' },
    { value: 'OUVERTE', label: 'Ouverte' },
    { value: 'ENVOYEE', label: 'Envoyée' },
    { value: 'EN_COURS_DETUDE', label: "En cours d'étude" },
    { value: 'RESOLU', label: 'Résolu' },
    { value: 'REJETE', label: 'Rejeté' },
  ];

  readonly prioriteOptions = [
    { value: '', label: 'Toutes les priorités' },
    { value: 'CRITIQUE', label: 'Critique' },
    { value: 'HAUTE', label: 'Haute' },
    { value: 'MOYENNE', label: 'Moyenne' },
    { value: 'FAIBLE', label: 'Faible' },
  ];

  readonly graviteOptions = [
    { value: '', label: 'Toutes les gravités' },
    { value: 'CRITIQUE', label: 'Critique' },
    { value: 'GRAVE', label: 'Grave' },
    { value: 'MODEREE', label: 'Modérée' },
    { value: 'FAIBLE', label: 'Faible' },
  ];

  private fb = inject(FormBuilder);

  filterForm = this.fb.group({
    statut: [''],
    priorite: [''],
    gravite: [''],
    date_from: [''],
    date_to: [''],
    onlyVisible: [true],
    limit: [1000],
  });

  private map: L.Map | null = null;
  private markerLayer: L.LayerGroup | null = null;
  private markerById = new Map<number, L.Marker>();
  private tunisiaMask: L.Polygon | null = null;
  private governorateLayer: L.GeoJSON | null = null;
  private moveDebounceId: ReturnType<typeof setTimeout> | null = null;
  private defaultIcon = L.icon({
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    iconSize: [25, 41],
    iconAnchor: [12, 41],
  });

  constructor(
    private mapSignalementsService: MapSignalementsService
  ) {}

  ngOnInit(): void {
    this.userName = this.resolveUserName();
    this.loading = false;
  }

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') return;
    this.initMap();
    this.loadMarkers();
  }

  ngOnDestroy(): void {
    if (this.moveDebounceId) {
      clearTimeout(this.moveDebounceId);
      this.moveDebounceId = null;
    }
    if (this.map) {
      this.map.off();
      this.map.remove();
      this.map = null;
    }
    this.markerById.clear();
    this.markerLayer = null;
    this.tunisiaMask = null;
    this.governorateLayer = null;
    this.currentReports = [];
  }

  applyFilters(): void {
    this.selectedDetail = null;
    this.loadMarkers();
  }

  resetFilters(): void {
    this.filterForm.reset({
      statut: '',
      priorite: '',
      gravite: '',
      date_from: '',
      date_to: '',
      onlyVisible: true,
      limit: 1000,
    });
    this.selectedDetail = null;
    this.loadMarkers();
  }

  refresh(): void {
    this.loadMarkers();
  }

  closeDetail(): void {
    this.selectedDetail = null;
  }

  hideBrokenImage(event: Event): void {
    const el = event.target as HTMLImageElement | null;
    if (el) el.style.display = 'none';
  }

  openDetailFromMarker(marker: MapReportMarker): void {
    this.isLoadingMarkers = true;
    this.error = null;
    this.mapSignalementsService.getReportDetail(marker.id).subscribe({
      next: (detail) => {
        this.selectedDetail = detail;
        this.isLoadingMarkers = false;
      },
      error: (err: any) => {
        this.isLoadingMarkers = false;
        this.error = err?.message || 'Erreur lors du chargement du détail.';
      }
    });
  }

  private initMap(): void {
    const bounds = L.latLngBounds(L.latLng(30.2, 7.5), L.latLng(37.55, 11.6));

    this.map = L.map('org-map', {
      center: [33.8869, 9.5375],
      zoom: 7,
      minZoom: 6,
      maxZoom: 18,
      maxBounds: bounds,
      maxBoundsViscosity: 1.0,
      zoomControl: true,
    });

    this.map.createPane('maskPane');
    this.map.getPane('maskPane')!.style.zIndex = '300';
    this.map.getPane('maskPane')!.style.pointerEvents = 'none';

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap',
      maxZoom: 18,
      bounds,
      noWrap: true,
    }).addTo(this.map);

    this.map.fitBounds(bounds);
    this.map.on('drag', () => this.map?.panInsideBounds(bounds, { animate: false }));

    this.markerLayer = L.layerGroup().addTo(this.map);

    void this.addTunisiaMask();
    void this.addGovernoratesLayer();

    this.map.on('moveend', () => {
      if (!this.filterForm.value.onlyVisible) return;
      if (this.moveDebounceId) clearTimeout(this.moveDebounceId);
      this.moveDebounceId = setTimeout(() => this.loadMarkers(), 200);
    });
  }

  private loadMarkers(): void {
    if (!this.map || !this.markerLayer) return;

    this.isLoadingMarkers = true;
    this.error = null;

    const formValue = this.filterForm.value;
    const bbox = formValue.onlyVisible ? this.buildBboxFromMap() : undefined;
    const limit = Math.min(Math.max(formValue.limit ?? 1000, 1), 2000);

    this.mapSignalementsService.getReports({
      statut: formValue.statut || undefined,
      priorite: formValue.priorite || undefined,
      gravite: formValue.gravite || undefined,
      date_from: formValue.date_from || undefined,
      date_to: formValue.date_to || undefined,
      bbox,
      limit,
      offset: 0,
    }).subscribe({
      next: (res) => {
        this.markerSummary = { total: res.total, returned: res.returned };
        this.currentReports = res.data;
        this.renderMarkers(res.data);
        this.isLoadingMarkers = false;
      },
      error: (err: any) => {
        this.isLoadingMarkers = false;
        this.error = err?.message || "Impossible de charger les signalements depuis le map-service.";
        this.markerSummary = { total: 0, returned: 0 };
        this.currentReports = [];
        this.clearMarkers();
      }
    });
  }

  private async addTunisiaMask(): Promise<void> {
    if (!this.map) return;
    if (this.tunisiaMask) return;

    try {
      const res = await fetch('/tunisia.json', { method: 'GET' });
      if (!res.ok) return;
      const geojson = await res.json();

      const worldCoords: [number, number][] = [
        [90, -180],
        [90, 180],
        [-90, 180],
        [-90, -180],
        [90, -180],
      ];

      const holes: [number, number][][] = [];

      if (geojson?.type === 'Polygon') {
        holes.push((geojson.coordinates?.[0] ?? []).map((c: [number, number]) => [c[1], c[0]]));
      } else if (geojson?.type === 'MultiPolygon') {
        for (const poly of (geojson.coordinates ?? [])) {
          holes.push((poly?.[0] ?? []).map((c: [number, number]) => [c[1], c[0]]));
        }
      } else {
        return;
      }

      this.tunisiaMask = L.polygon([worldCoords, ...holes], {
        pane: 'maskPane',
        color: '#38bdf8',
        weight: 2,
        fillColor: '#ffffff',
        fillOpacity: 1,
        interactive: false,
      }).addTo(this.map);
    } catch {
      return;
    }
  }

  private async addGovernoratesLayer(): Promise<void> {
    if (!this.map) return;
    if (this.governorateLayer) return;

    try {
      const url = 'https://raw.githubusercontent.com/mtimet/tnacmaps/master/geojson/governorates.geojson';
      const res = await fetch(url, { method: 'GET' });
      if (!res.ok) return;
      const geojson = await res.json();

      this.governorateLayer = L.geoJSON(geojson, {
        style: () => ({
          color: '#60a5fa',
          weight: 1,
          fillColor: '#000000',
          fillOpacity: 0.01,
        }),
        onEachFeature: (feature: any, layer: any) => {
          layer.on('mouseover', (e: any) => {
            layer.setStyle({ weight: 2, color: '#93c5fd', fillOpacity: 0.08 });
            const name = this.getGovernorateName(feature);
            const stats = this.computeGovernorateStats(feature);
            layer.bindTooltip(this.governorateTooltipHtml(name, stats), {
              sticky: true,
              direction: 'top',
              className: 'gov-tooltip',
              opacity: 1,
            }).openTooltip(e.latlng);
          });

          layer.on('mouseout', () => {
            layer.setStyle({ weight: 1, color: '#60a5fa', fillOpacity: 0.01 });
            layer.closeTooltip();
          });
        },
      }).addTo(this.map);
    } catch {
      return;
    }
  }

  private getGovernorateName(feature: any): string {
    const p = feature?.properties || {};
    const raw = p.gov_name_f || p.gouv_fr || p.nom_fr || p.name || p.NAME_1 || p.gouv_ar || 'Gouvernorat';
    const normalized = String(raw)
      .trim()
      .toLowerCase()
      .replace(/\s+/g, ' ')
      .split(' ')
      .map((w) => w.charAt(0).toUpperCase() + w.slice(1))
      .join(' ');

    const fixes: Record<string, string> = {
      'Le Kef': 'Le Kef',
      'Kef': 'Le Kef',
      'Sidi Bouzid': 'Sidi Bouzid',
      'Sidi Bou Zid': 'Sidi Bouzid',
      'Kairouan': 'Kairouan',
      'Ben Arous': 'Ben Arous',
      'Manubah': 'Manouba',
      'Beja': 'Beja',
      'Béja': 'Beja',
      'Kasserine': 'Kasserine',
      'Kassérine': 'Kasserine',
      'Medenine': 'Medenine',
      'Médenine': 'Medenine',
      'Gabes': 'Gabes',
      'Gabés': 'Gabes',
    };

    return fixes[normalized] || normalized;
  }

  private pointInRing(lon: number, lat: number, ring: number[][]): boolean {
    let inside = false;
    for (let i = 0, j = ring.length - 1; i < ring.length; j = i++) {
      const xi = ring[i][0], yi = ring[i][1];
      const xj = ring[j][0], yj = ring[j][1];
      const intersect = ((yi > lat) !== (yj > lat))
        && (lon < ((xj - xi) * (lat - yi)) / ((yj - yi) || Number.EPSILON) + xi);
      if (intersect) inside = !inside;
    }
    return inside;
  }

  private pointInPolygonWithHoles(lon: number, lat: number, polygonCoords: number[][][]): boolean {
    if (!polygonCoords || polygonCoords.length === 0) return false;
    if (!this.pointInRing(lon, lat, polygonCoords[0])) return false;
    for (let i = 1; i < polygonCoords.length; i++) {
      if (this.pointInRing(lon, lat, polygonCoords[i])) return false;
    }
    return true;
  }

  private pointInFeature(lon: number, lat: number, feature: any): boolean {
    const geom = feature?.geometry;
    if (!geom) return false;
    if (geom.type === 'Polygon') {
      return this.pointInPolygonWithHoles(lon, lat, geom.coordinates);
    }
    if (geom.type === 'MultiPolygon') {
      return (geom.coordinates || []).some((poly: any) => this.pointInPolygonWithHoles(lon, lat, poly));
    }
    return false;
  }

  private computeGovernorateStats(feature: any): { total: number; byType: Array<[string, number]> } {
    const rows = this.currentReports.filter(r => this.pointInFeature(r.longitude, r.latitude, feature));
    const byType: Record<string, number> = {};
    for (const r of rows) {
      const key = r.domaine || 'AUTRE';
      byType[key] = (byType[key] || 0) + 1;
    }
    const sorted = Object.entries(byType).sort((a, b) => b[1] - a[1]);
    return { total: rows.length, byType: sorted };
  }

  private governorateTooltipHtml(name: string, stats: { total: number; byType: Array<[string, number]> }): string {
    const typesHtml = stats.byType.length
      ? stats.byType.map(([k, v]) => `<div>${this.escapeHtml(k)}: <b>${v}</b></div>`).join('')
      : '<div>Aucun signalement</div>';
    return `
      <div class="gov-card">
        <div class="gov-title">${this.escapeHtml(name)}</div>
        <div class="gov-total">Total signalements: <b>${stats.total}</b></div>
        <div class="gov-types">${typesHtml}</div>
      </div>
    `;
  }

  private buildBboxFromMap(): string | undefined {
    if (!this.map) return undefined;
    const b = this.map.getBounds();
    const latMin = b.getSouthWest().lat;
    const lonMin = b.getSouthWest().lng;
    const latMax = b.getNorthEast().lat;
    const lonMax = b.getNorthEast().lng;
    return `${latMin},${lonMin},${latMax},${lonMax}`;
  }

  private clearMarkers(): void {
    if (!this.markerLayer) return;
    this.markerLayer.clearLayers();
    this.markerById.clear();
  }

  private renderMarkers(markers: MapReportMarker[]): void {
    if (!this.markerLayer) return;
    this.clearMarkers();

    for (const m of markers) {
      const color = this.getMarkerColor(m);
      const icon = this.createPinIcon(color);
      const leafletMarker = L.marker([m.latitude, m.longitude], { icon: icon ?? this.defaultIcon });

      leafletMarker.on('click', () => this.openDetailFromMarker(m));
      leafletMarker.bindTooltip(
        `<b>#${m.id}</b> — ${m.domaine ?? 'N/A'}<br>${this.escapeHtml(m.description_short)}`,
        { direction: 'top', offset: [0, -30] }
      );

      leafletMarker.addTo(this.markerLayer);
      this.markerById.set(m.id, leafletMarker);
    }
  }

  private getMarkerColor(marker: MapReportMarker): string {
    if (marker.statut === 'RESOLU') return '#22c55e';
    switch (marker.priorite) {
      case 'CRITIQUE': return '#ef4444';
      case 'HAUTE': return '#f97316';
      case 'MOYENNE': return '#eab308';
      case 'FAIBLE': return '#22c55e';
      default: return '#94a3b8';
    }
  }

  private createPinIcon(color: string): L.DivIcon | null {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 32" width="20" height="26">
      <ellipse cx="12" cy="30" rx="3.5" ry="1.5" fill="#000000" opacity="0.32"/>
      <path d="M10.5 15 L11.5 30 A0.5 0.5 0 0 0 12.5 30 L13.5 15 Z" fill="#cbd5e1" stroke="#94a3b8" stroke-width="0.5"/>
      <circle cx="12" cy="10" r="9" fill="${color}" stroke="#ffffff" stroke-width="1.5"/>
      <path d="M7 6 A 4.5 4.5 0 0 1 11 4" stroke="rgba(255,255,255,0.6)" stroke-width="1.5" stroke-linecap="round" fill="none" />
    </svg>`;
    return L.divIcon({
      html: svg,
      className: '',
      iconSize: [20, 26],
      iconAnchor: [10, 26],
      popupAnchor: [0, -26],
    });
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private resolveUserName(): string {
    const keys = ['full_name', 'username', 'userName', 'nomOrganisation', 'nom', 'email'];
    for (const key of keys) {
      const value = localStorage.getItem(key);
      if (value && value.trim()) {
        return value.trim();
      }
    }
    return 'CivicLens Organisation';
  }
}
