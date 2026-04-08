import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map as import_rxjs_map } from 'rxjs/operators';
import { ApiService } from './api.service';
import { environment } from '../../../environments/environment';
import {
  Signalement,
  CreateSignalementRequest,
  SignalementStats
} from '../../shared/models/signalement.model';

@Injectable({
  providedIn: 'root'
})
export class SignalementService {
  constructor(private api: ApiService) {}

  private prepareImages(s: Signalement): Signalement {
    const baseUrl = environment.apiUrl.replace('/api', '');
    if (s.imageUrl && !s.imageUrl.startsWith('http') && !s.imageUrl.startsWith('data:')) {
      s.imageUrl = `${baseUrl}${s.imageUrl.startsWith('/') ? '' : '/'}${s.imageUrl}`;
    }
    if (s.imagesUrl && s.imagesUrl.length > 0) {
      s.imagesUrl = s.imagesUrl.map(img => 
        (!img.startsWith('http') && !img.startsWith('data:')) 
          ? `${baseUrl}${img.startsWith('/') ? '' : '/'}${img}` 
          : img
      );
    }
    return s;
  }

  // Mes signalements (citoyen connecté)
  getMesSignalements(): Observable<Signalement[]> {
    return this.api.get<Signalement[]>('/signalements/mes').pipe(
      import_rxjs_map(list => list.map(s => this.prepareImages(s)))
    );
  }

  // Signalements de ma délégation
  getSignalementsDelegation(delegation: string): Observable<Signalement[]> {
    return this.api.get<Signalement[]>(`/signalements/delegation?nomDelegation=${encodeURIComponent(delegation)}`).pipe(
      import_rxjs_map(list => list.map(s => this.prepareImages(s)))
    );
  }

  // Obtenir un signalement par ID
  getSignalementById(id: string): Observable<Signalement> {
    return this.api.get<any>(`/signalements/${id}`).pipe(
      import_rxjs_map(res => {
        const s = {
          ...res.signalement,
          imagesUrl: res.images
        };
        return this.prepareImages(s);
      })
    );
  }

  // Créer un signalement
  createSignalement(data: CreateSignalementRequest): Observable<Signalement> {
    const formData = new FormData();
    formData.append('description', data.description);
    formData.append('latitude', data.latitude.toString());
    formData.append('longitude', data.longitude.toString());
    
    if (data.images && data.images.length > 0) {
      for (const img of data.images) {
        formData.append('images', img); // Multiple files under same key
      }
    }
    
    return this.api.post<Signalement>('/signalements', formData).pipe(
      import_rxjs_map(s => this.prepareImages(s))
    );
  }

  // Supprimer un signalement
  deleteSignalement(id: string): Observable<void> {
    return this.api.delete<void>(`/signalements/${id}`);
  }

  // Voter pour un signalement
  voterSignalement(id: string): Observable<Signalement> {
    return this.api.post<Signalement>(`/signalements/${id}/vote`, {}).pipe(
      import_rxjs_map(s => this.prepareImages(s))
    );
  }

  // Statistiques citoyen
  getMesStats(): Observable<SignalementStats> {
    return this.api.get<SignalementStats>('/signalements/mes/stats');
  }
}