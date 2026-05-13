import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface BureauStatsResponse {
  total: number;
  enAttente: number;
  ouvertes: number;
  envoyees: number;
  enCoursDEtude: number;
  resolus: number;
  rejetes: number;
  domaine: string | null;
  delegation: string;
}

@Injectable({ providedIn: 'root' })
export class BureauDashboardService {
  private readonly baseUrl = `${environment.apiUrl}/bureau`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<BureauStatsResponse> {
    return this.http.get<BureauStatsResponse>(`${this.baseUrl}/stats`);
  }
}

