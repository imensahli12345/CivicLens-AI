import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

export interface BureauProfile {
  id: string;
  email: string;
  region: string;
  delegation: string;
  matricule: string;
}

export type ProfileRecord = Record<string, string | number | null | undefined>;
export type ProfileType = 'bureau' | 'service' | 'organisation';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {
  constructor(private apiService: ApiService) {}

  getBureauProfile(): Observable<BureauProfile> {
    return this.apiService.get<BureauProfile>('/bureau/me');
  }

  getProfile<T extends ProfileRecord>(profileType: ProfileType, endpoint?: string): Observable<T> {
    const resolvedEndpoint = endpoint ?? this.resolveEndpoint(profileType);
    return this.apiService.get<T>(resolvedEndpoint);
  }

  private resolveEndpoint(profileType: ProfileType): string {
    const endpointMap: Record<ProfileType, string> = {
      bureau: '/bureau/me',
      service: '/service/me',
      organisation: '/organisation/me'
    };

    return endpointMap[profileType];
  }
}
