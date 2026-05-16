import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface BureauSignalement {
  id: number;
  titre: string;
  description: string;
  descriptionService: string;
  dateCreation: string;
  dateResolution: string | null;
  statut: string;
  gravite: string;
  priorite: string;
  domaine: string;
  adresse: string;
  cinCitoyen: string;
  organisationNom: string;
  images: string[];
}

export interface OrganisationOption {
  id: string;
  nomOrganisation: string;
}

export interface BureauSignalementFilters {
  statut?: string | null;
  gravite?: string | null;
  priorite?: string | null;
  dateResolutionFrom?: string | null;
  dateResolutionTo?: string | null;
}

export interface AssignSignalementPayload {
  organisationId: string;
  organisationNom?: string | null;
  descriptionSupplementaire?: string | null;
}

export interface HistoriqueEntry {
  dateChangement: string;
  statut: string;
  modificateur: string | null;
}

interface RawOrganisation {
  id?: unknown;
  Id?: unknown;
  organisationId?: unknown;
  OrganisationId?: unknown;
  nomOrganisation?: unknown;
  NomOrganisation?: unknown;
  nom?: unknown;
  Nom?: unknown;
}

interface OrganisationsResponse {
  organisations?: RawOrganisation[];
  data?: RawOrganisation[];
  value?: RawOrganisation[];
}

@Injectable({ providedIn: 'root' })
export class BureauSignalementsService {
  private readonly baseUrl = `${environment.apiUrl}/bureau/signalements`;

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

  assignerSignalement(id: number, payload: AssignSignalementPayload): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/${id}/envoyer`,
      {
        organisationId: payload.organisationId,
        descriptionSupplementaire: payload.descriptionSupplementaire ?? null
      },
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

  downloadSignalementPdf(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  getOrganisations(): Observable<OrganisationOption[]> {
    return this.http
      .get<RawOrganisation[] | OrganisationsResponse>(`${environment.apiUrl}/bureau/organisations`)
      .pipe(
        map((res) => {
          const items = Array.isArray(res) ? res : (res.organisations ?? res.data ?? res.value ?? []);
          return items.map((item) => this.normalizeOrganisation(item)).filter((item) => !!item.id);
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

  private normalizeOrganisation(raw: RawOrganisation): OrganisationOption {
    const id =
      this.normalizeText(raw.id) ||
      this.normalizeText(raw.Id) ||
      this.normalizeText(raw.organisationId) ||
      this.normalizeText(raw.OrganisationId);

    const nomOrganisation =
      this.normalizeText(raw.nomOrganisation) ||
      this.normalizeText(raw.NomOrganisation) ||
      this.normalizeText(raw.nom) ||
      this.normalizeText(raw.Nom) ||
      'Organisation';

    return { id, nomOrganisation };
  }

  private normalizeText(value: unknown): string {
    if (typeof value === 'string') return value.trim();
    if (typeof value === 'number') return String(value);
    return '';
  }
}
