import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BureauStatsResponse } from './bureau-dashboard.service';

@Injectable({ providedIn: 'root' })
export class ServiceDashboardService {
  private readonly baseUrl = `${environment.apiUrl}/service`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<BureauStatsResponse> {
    return this.http.get<BureauStatsResponse>(`${this.baseUrl}/stats`);
  }
}
