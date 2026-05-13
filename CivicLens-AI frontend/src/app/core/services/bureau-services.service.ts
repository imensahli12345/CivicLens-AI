import { HttpClient } from '@angular/common/http';
import { HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BureauService {
  id: string;
  nom: string;
  email: string;
  domaine: string;
  statut: 'ACTIF' | 'INACTIF';
}

interface BureauServicesResponse {
  bureauId: string;
  bureauEmail: string;
  services?: RawBureauService[];
  data?: RawBureauService[];
}

type RawBureauService = Record<string, unknown>;

@Injectable({ providedIn: 'root' })
export class BureauServicesService {
  private readonly baseUrl = `${environment.apiUrl}/bureau`;

  constructor(private http: HttpClient) {}

  getServices(): Observable<BureauService[]> {
    return this.http
      .get<BureauServicesResponse | RawBureauService[]>(`${this.baseUrl}/services`)
      .pipe(
        map((res) => {
          const items = Array.isArray(res) ? res : (res.services ?? res.data ?? []);
          return items.map((item) => this.normalizeService(item));
        })
      );
  }

  activate(serviceId: string): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/services/${serviceId}/activer`,
      {},
      { observe: 'response', responseType: 'text' }
    );
  }

  deactivate(serviceId: string): Observable<HttpResponse<string>> {
    return this.http.put(
      `${this.baseUrl}/services/${serviceId}/desactiver`,
      {},
      { observe: 'response', responseType: 'text' }
    );
  }

  private normalizeService(raw: RawBureauService): BureauService {
    const rawStatus = this.readField(raw, ['statut', 'Statut', 'status', 'Status']);
    return {
      id: this.normalizeText(this.readField(raw, ['id', 'Id', 'serviceId', 'ServiceId']), ''),
      nom: this.normalizeText(this.readField(raw, ['nom', 'Nom', 'name', 'Name']), 'Service'),
      email: this.normalizeText(this.readField(raw, ['email', 'Email', 'mail', 'Mail']), '-'),
      domaine: this.normalizeLabel(
        this.readField(raw, ['domaine', 'Domaine', 'categorie', 'Categorie', 'nomDomaine', 'NomDomaine']),
        'Non defini'
      ),
      statut: this.normalizeStatus(rawStatus)
    };
  }

  private normalizeStatus(value: unknown): 'ACTIF' | 'INACTIF' {
    const normalized = this.normalizeText(value, '').replace(/[\s-]+/g, '_').toUpperCase();
    return normalized === 'ACTIF' || normalized === 'ACTIVE' ? 'ACTIF' : 'INACTIF';
  }

  private normalizeLabel(value: unknown, fallback: string): string {
    const text = this.normalizeText(value, fallback);
    return text.replace(/[_-]+/g, ' ').replace(/\s+/g, ' ').trim();
  }

  private normalizeText(value: unknown, fallback: string): string {
    if (typeof value === 'string') {
      const trimmed = value.trim();
      return trimmed || fallback;
    }

    if (typeof value === 'number') {
      return String(value);
    }

    return fallback;
  }

  private readField(source: RawBureauService, keys: string[]): unknown {
    for (const key of keys) {
      const value = source[key];
      if (value !== undefined && value !== null) {
        return value;
      }
    }
    return undefined;
  }
}
