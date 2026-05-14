import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface MapReportMarker {
  id: number;
  latitude: number;
  longitude: number;
  statut?: string | null;
  priorite?: string | null;
  gravite?: string | null;
  domaine?: string | null;
  score_ai?: number | null;
  date_creation: string;
  vote_count: number;
  has_images: boolean;
  description_short: string;
}

export interface MapReportsResponse {
  total: number;
  returned: number;
  offset: number;
  data: MapReportMarker[];
}

export interface MapImage {
  id: string;
  url?: string | null;
}

export interface MapReportDetail {
  id: number;
  description: string;
  statut?: string | null;
  priorite?: string | null;
  gravite?: string | null;
  domaine?: string | null;
  score_ai?: number | null;
  adresse?: unknown;
  latitude?: number | null;
  longitude?: number | null;
  date_creation: string;
  citoyen_id: string;
  organisation_id?: string | null;
  vote_count: number;
  images: MapImage[];
}

export interface MapStats {
  total: number;
  by_statut: Record<string, number>;
  by_priorite: Record<string, number>;
  by_domaine: Record<string, number>;
  avg_score_ai?: number | null;
}

export interface MapFilters {
  statut?: string;
  priorite?: string;
  gravite?: string;
  domaine?: string;
  min_score?: number;
  date_from?: string;
  date_to?: string;
  bbox?: string;
  limit?: number;
  offset?: number;
}

@Injectable({ providedIn: 'root' })
export class MapSignalementsService {
  private baseUrl = environment.mapServiceUrl;

  constructor(private http: HttpClient) {}

  getReports(filters: MapFilters): Observable<MapReportsResponse> {
    let params = new HttpParams();
    const entries: Array<[keyof MapFilters, MapFilters[keyof MapFilters]]> = Object.entries(filters) as any;
    for (const [key, value] of entries) {
      if (value === undefined || value === null || value === '') continue;
      params = params.set(String(key), String(value));
    }
    return this.http.get<MapReportsResponse>(`${this.baseUrl}/map/reports`, { params });
  }

  getReportDetail(id: number): Observable<MapReportDetail> {
    return this.http.get<MapReportDetail>(`${this.baseUrl}/map/reports/${id}`);
  }

  getStats(): Observable<MapStats> {
    return this.http.get<MapStats>(`${this.baseUrl}/map/stats`);
  }
}
