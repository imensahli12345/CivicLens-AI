import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  AssignSignalementPayload,
  BureauSignalement,
  BureauSignalementFilters,
  HistoriqueEntry
} from './bureau-signalements.service';

@Injectable({ providedIn: 'root' })
export class ServiceSignalementsService {
  private readonly baseUrl = `${environment.apiUrl}/service/signalements`;

  constructor(private http: HttpClient) {}

  getSignalements(filters?: BureauSignalementFilters | null): Observable<BureauSignalement[]> {
    const normalizedFilters = this.normalizeFilters(filters);
    const hasFilters = Object.keys(normalizedFilters).length > 0;

    if (hasFilters) {
      let params = new HttpParams();

      Object.entries(normalizedFilters).forEach(([key, value]) => {
        params = params.set(this.toApiParamName(key), value);
      });

      return this.http
        .get<BureauSignalement[]>(this.baseUrl, { params })
        .pipe(map((list) => (list ?? []).map((item) => this.normalizeSignalement(item))));
    }

    return this.http
      .get<BureauSignalement[]>(`${this.baseUrl}/tous`)
      .pipe(map((list) => (list ?? []).map((item) => this.normalizeSignalement(item))));
  }

  getSignalementById(id: number): Observable<BureauSignalement> {
    return this.http
      .get<BureauSignalement>(`${this.baseUrl}/${id}`)
      .pipe(map((item) => this.normalizeSignalement(item)));
  }

  ouvrirSignalement(id: number): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/${id}/ouvrir`,
      {},
      { observe: 'response', responseType: 'text' }
    );
  }

  envoyerSignalement(id: number, payload?: AssignSignalementPayload): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/${id}/envoyer`,
      payload
        ? {
            organisationId: payload.organisationId,
            descriptionSupplementaire: payload.descriptionSupplementaire ?? null
          }
        : {},
      { observe: 'response', responseType: 'text' }
    );
  }

  rejeterSignalement(id: number): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/${id}/rejeter`,
      {},
      { observe: 'response', responseType: 'text' }
    );
  }

  getHistoriqueSignalement(id: number): Observable<HistoriqueEntry[]> {
    return this.http
      .get<HistoriqueEntry[] | { value?: HistoriqueEntry[] }>(`${this.baseUrl}/${id}/historique`)
      .pipe(
        map((res) => {
          const items = Array.isArray(res) ? res : (res.value ?? []);
          return (items ?? []).map((item) => ({
            dateChangement: item.dateChangement,
            statut: item.statut,
            modificateur: item.modificateur
          }));
        })
      );
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
