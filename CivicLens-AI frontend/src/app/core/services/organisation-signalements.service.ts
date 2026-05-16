import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  BureauSignalement,
  BureauSignalementFilters
} from './bureau-signalements.service';

export interface OrganisationSignalementActionResponse {
  message: string;
  signalementId: number;
  statut: string;
}

export interface OrganisationRapportUploadResponse {
  message: string;
}

export interface OrganisationSignalementActionApi {
  getSignalements(filters?: BureauSignalementFilters | null): Observable<BureauSignalement[]>;
  getSignalementById(id: number): Observable<BureauSignalement>;
  ouvrirSignalement(id: number): Observable<HttpResponse<OrganisationSignalementActionResponse>>;
  resoudreSignalement(id: number): Observable<HttpResponse<unknown>>;
  uploadRapport(id: number, file: File): Observable<OrganisationRapportUploadResponse>;
  downloadSignalementPdf(id: number): Observable<Blob>;
}

@Injectable({ providedIn: 'root' })
export class OrganisationSignalementsService implements OrganisationSignalementActionApi {
  private readonly baseUrl = `${environment.apiUrl}/organisation/signalements`;

  constructor(private http: HttpClient) {}

  getAllSignalements(): Observable<BureauSignalement[]> {
    return this.http
      .get<BureauSignalement[]>(`${this.baseUrl}/tous`)
      .pipe(map((list) => (list ?? []).map((item) => this.normalizeSignalement(item))));
  }

  getFilteredSignalements(filters?: BureauSignalementFilters | null): Observable<BureauSignalement[]> {
    const normalizedFilters = this.normalizeFilters(filters);
    let params = new HttpParams();

    Object.entries(normalizedFilters).forEach(([key, value]) => {
      params = params.set(this.toApiParamName(key), value);
    });

    return this.http
      .get<BureauSignalement[]>(this.baseUrl, { params })
      .pipe(map((list) => (list ?? []).map((item) => this.normalizeSignalement(item))));
  }

  getSignalements(filters?: BureauSignalementFilters | null): Observable<BureauSignalement[]> {
    const normalizedFilters = this.normalizeFilters(filters);
    return Object.keys(normalizedFilters).length > 0
      ? this.getFilteredSignalements(normalizedFilters)
      : this.getAllSignalements();
  }

  getSignalementById(id: number): Observable<BureauSignalement> {
    return this.http
      .get<BureauSignalement>(`${this.baseUrl}/${id}`)
      .pipe(map((item) => this.normalizeSignalement(item)));
  }

  ouvrirSignalement(id: number): Observable<HttpResponse<OrganisationSignalementActionResponse>> {
    return this.http.put<OrganisationSignalementActionResponse>(
      `${this.baseUrl}/${id}/ouvrir`,
      {},
      { observe: 'response' }
    );
  }

  resoudreSignalement(id: number): Observable<HttpResponse<unknown>> {
    return this.http.put(
      `${this.baseUrl}/${id}/resoudre`,
      {},
      { observe: 'response' }
    );
  }

  uploadRapport(id: number, file: File): Observable<OrganisationRapportUploadResponse> {
    const formData = new FormData();
    formData.append('Rapport', file);

    return this.http.put<OrganisationRapportUploadResponse>(
      `${this.baseUrl}/${id}/rapport`,
      formData
    );
  }

  downloadSignalementPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  private normalizeSignalement(item: BureauSignalement): BureauSignalement {
    return {
      ...item,
      domaine: item.domaine ?? '',
      statut: this.normalizeStatut(item.statut)
    };
  }

  private normalizeStatut(statut: string): string {
    const value = (statut ?? '').trim();
    if (value === 'RESOLUE') return 'RESOLU';
    if (value === 'REJETE') return 'REJETEE';
    if (value === 'EN_COURS_DETUDE') return 'EN_COURS';
    return value;
  }

  private normalizeFilters(filters?: BureauSignalementFilters | null): Record<string, string> {
    if (!filters) return {};

    const normalizedEntries = Object.entries(filters)
      .map(([key, value]) => [key, typeof value === 'string' ? value.trim() : value] as const)
      .filter(([, value]) => !!value);

    return Object.fromEntries(normalizedEntries) as Record<string, string>;
  }

  private toApiParamName(key: string): string {
    const map: Record<string, string> = {
      statut: 'Statut',
      gravite: 'Gravite',
      priorite: 'Priorite',
      dateResolutionFrom: 'DateResolutionFrom',
      dateResolutionTo: 'DateResolutionTo'
    };

    return map[key] ?? key;
  }
}
