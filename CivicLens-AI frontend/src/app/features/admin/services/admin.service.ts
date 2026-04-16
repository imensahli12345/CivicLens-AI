import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = 'http://localhost:5242/api';

  constructor(private http: HttpClient) {}

  private getHeaders() {
    const token = localStorage.getItem('access_token');
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  getCitoyens(filters?: { region?: string; delegation?: string; statut?: string }): Observable<any[]> {
    let params = new HttpParams();
    if (filters?.region) params = params.set('region', filters.region);
    if (filters?.delegation) params = params.set('delegation', filters.delegation);
    if (filters?.statut) params = params.set('statut', filters.statut);
    return this.http.get<any[]>(`${this.baseUrl}/admin/citoyens`, { headers: this.getHeaders(), params });
  }

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/stats`, { headers: this.getHeaders() });
  }

  getStatsTimeseries(days = 30): Observable<any> {
    const params = new HttpParams().set('days', String(days));
    return this.http.get<any>(`${this.baseUrl}/admin/stats/timeseries`, { headers: this.getHeaders(), params });
  }

  createBureau(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/create-bureau`, data, {
      headers: this.getHeaders(),
      responseType: 'text'
    });
  }

  createOrganisation(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/create-organisation`, data, {
      headers: this.getHeaders(),
      responseType: 'text'
    });
  }

  getBureaux(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/bureaux`, { headers: this.getHeaders() });
  }

  getBureauxFiltered(filters?: { region?: string; delegation?: string }): Observable<any[]> {
    let params = new HttpParams();
    if (filters?.region) params = params.set('region', filters.region);
    if (filters?.delegation) params = params.set('delegation', filters.delegation);
    return this.http.get<any[]>(`${this.baseUrl}/admin/bureaux`, { headers: this.getHeaders(), params });
  }

  getOrganisations(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/organisations`, { headers: this.getHeaders() });
  }

  getOrganisationsFiltered(filters?: { domaine?: string; type?: string }): Observable<any[]> {
    let params = new HttpParams();
    if (filters?.domaine) params = params.set('domaine', filters.domaine);
    if (filters?.type) params = params.set('type', filters.type);
    return this.http.get<any[]>(`${this.baseUrl}/admin/organisations`, { headers: this.getHeaders(), params });
  }

  updateBureauStatut(id: string, statut: 'ACTIF' | 'INACTIF'): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/bureaux/${id}/statut`, { statut }, { headers: this.getHeaders() });
  }

  updateOrganisationStatut(id: string, statut: 'ACTIF' | 'INACTIF'): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/organisations/${id}/statut`, { statut }, { headers: this.getHeaders() });
  }
}
