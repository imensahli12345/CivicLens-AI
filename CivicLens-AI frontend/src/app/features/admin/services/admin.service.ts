import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = environment.apiUrl;

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

  getBureauById(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/bureaux/${encodeURIComponent(id)}`, { headers: this.getHeaders() });
  }

  getOrganisationById(id: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/organisations/${encodeURIComponent(id)}`, { headers: this.getHeaders() });
  }

  updateBureauDetails(id: string, payload: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/bureaux/${encodeURIComponent(id)}`, payload, { headers: this.getHeaders() });
  }

  updateOrganisationDetails(id: string, payload: any): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/organisations/${encodeURIComponent(id)}`, payload, { headers: this.getHeaders() });
  }

  deleteBureau(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/admin/bureaux/${encodeURIComponent(id)}`, { headers: this.getHeaders() });
  }

  deleteOrganisation(id: string): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/admin/organisations/${encodeURIComponent(id)}`, { headers: this.getHeaders() });
  }

  downloadAppStatsPdf(days = 30): Observable<Blob> {
    const params = new HttpParams().set('days', String(days));
    return this.http.get(`${this.baseUrl}/admin/stats/pdf`, { headers: this.getHeaders(), params, responseType: 'blob' });
  }

  downloadOrganisationsPdf(filters?: { domaine?: string; type?: string; q?: string }): Observable<Blob> {
    let params = new HttpParams();
    if (filters?.domaine) params = params.set('domaine', filters.domaine);
    if (filters?.type) params = params.set('type', filters.type);
    if (filters?.q) params = params.set('q', filters.q);
    return this.http.get(`${this.baseUrl}/admin/organisations/pdf`, { headers: this.getHeaders(), params, responseType: 'blob' });
  }

  downloadBureauxPdf(filters?: { region?: string; delegation?: string }): Observable<Blob> {
    let params = new HttpParams();
    if (filters?.region) params = params.set('region', filters.region);
    if (filters?.delegation) params = params.set('delegation', filters.delegation);
    return this.http.get(`${this.baseUrl}/admin/bureaux/pdf`, { headers: this.getHeaders(), params, responseType: 'blob' });
  }

  updateBureauStatut(id: string, statut: 'ACTIF' | 'INACTIF'): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/bureaux/${id}/statut`, { statut }, { headers: this.getHeaders() });
  }

  updateOrganisationStatut(id: string, statut: 'ACTIF' | 'INACTIF'): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/organisations/${id}/statut`, { statut }, { headers: this.getHeaders() });
  }

  getPlatformReclamations(filters?: { status?: string }): Observable<any[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    return this.http.get<any[]>(`${this.baseUrl}/admin/reclamations`, { headers: this.getHeaders(), params });
  }

  updateReclamationStatus(id: string, status: string): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/reclamations/${encodeURIComponent(id)}/status`, { status }, { headers: this.getHeaders() });
  }

  getActivationRequests(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/activation-requests`, { headers: this.getHeaders() });
  }

  approveActivationRequest(notificationId: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/admin/activation-requests/${encodeURIComponent(notificationId)}/approve`, {}, { headers: this.getHeaders() });
  }

  changeMyPassword(data: { ancienMotDePasse: string; nouveauMotDePasse: string }): Observable<any> {
    return this.http.put<any>(`${this.baseUrl}/admin/me/password`, data, { headers: this.getHeaders() });
  }
}
