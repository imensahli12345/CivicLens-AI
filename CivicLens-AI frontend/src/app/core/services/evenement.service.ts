import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Evenement, CreateEvenementRequest } from '../../shared/models/evenement.model';
import { CreateParticipationRequest } from '../../shared/models/participation.model';

@Injectable({
  providedIn: 'root'
})
export class EvenementService {
  constructor(private api: ApiService) {}

  getAll(): Observable<Evenement[]> {
    return this.api.get<Evenement[]>('/evenements');
  }

  getMes(): Observable<Evenement[]> {
    return this.api.get<Evenement[]>('/evenements/mes');
  }

  getRegion(): Observable<Evenement[]> {
    return this.api.get<Evenement[]>('/evenements/region');
  }

  getById(id: string): Observable<Evenement> {
    return this.api.get<Evenement>(`/evenements/${id}`);
  }

  create(data: CreateEvenementRequest): Observable<any> {
    return this.api.post<any>('/evenements', data);
  }

  update(id: string, data: { dateEvenement: Date | string; lieu: string }): Observable<any> {
    return this.api.put<any>(`/evenements/${id}`, data);
  }

  deleteEvenement(id: string): Observable<any> {
    return this.api.delete<any>(`/evenements/${id}`);
  }

  confirmEvenement(id: string): Observable<any> {
    return this.api.post<any>(`/evenements/${id}/confirm`, {});
  }

  participer(id: string, data: CreateParticipationRequest): Observable<any> {
    return this.api.post<any>(`/evenements/${id}/participer`, data);
  }

  annulerParticipation(id: string): Observable<any> {
    return this.api.delete<any>(`/evenements/${id}/participer`);
  }
}
