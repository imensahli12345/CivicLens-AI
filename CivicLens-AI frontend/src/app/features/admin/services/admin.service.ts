import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = 'http://localhost:5242/api';

  constructor(private http: HttpClient) {}

  private getHeaders() {
    const token = localStorage.getItem('token');
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  getCitoyens(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/citoyens`, { headers: this.getHeaders() });
  }

  updateCitoyenStatut(id: string, statut: string): Observable<any> {
    return this.http.put(`${this.baseUrl}/admin/citoyens/${id}/statut`, JSON.stringify(statut), {
      headers: this.getHeaders().set('Content-Type', 'application/json')
    });
  }

  getStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/admin/stats`, { headers: this.getHeaders() });
  }

  createBureau(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/create-bureau`, data, { headers: this.getHeaders() });
  }

  createOrganisation(data: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/admin/create-organisation`, data, { headers: this.getHeaders() });
  }

  getBureaux(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/admin/bureaux`, { headers: this.getHeaders() });
  }
}