import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { LoginRequest, RegisterRequest, User } from '../../shared/models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  constructor(private apiService: ApiService, private http: HttpClient) {}

  login(credentials: LoginRequest): Observable<string> {
    return this.http.post(
      `${environment.apiUrl}/auth/login`,
      {
        email: credentials.email,
        motDePasse: credentials.password
      },
      { responseType: 'text' }
    ).pipe(
      tap(response => {
        let token = response;
        try {
          // Tenter de parser si c'est du JSON
          const json = JSON.parse(response);
          token = json.token || json.accessToken || json.jwt || response;
        } catch {
          // Sinon, on garde la chaîne brute
        }
        localStorage.setItem('access_token', token);
        localStorage.removeItem('current_user'); // Forcer le rafraîchissement des données du nouvel utilisateur
      })
    );
  }

  register(userData: RegisterRequest): Observable<string> {
    return this.http.post(
      `${environment.apiUrl}/auth/register`,
      {
        nom: userData.nom,
        prenom: userData.prenom,
        email: userData.email,
        motDePasse: userData.password,
        region: userData.region,
        delegation: userData.delegation,
        cin: userData.cin
      },
      { responseType: 'text' }
    );
  }

  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('current_user');
    localStorage.removeItem('phone-notification-dismissed');
  }

  getToken(): string | null {
    return localStorage.getItem('access_token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  private decodeToken(token: string): any {
    try {
      const payload = token.split('.')[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  getCurrentUser(): User | null {
    const token = this.getToken();
    if (!token) return null;
    const payload = this.decodeToken(token);
    if (!payload) return null;

    // Extraire l'ID depuis plusieurs clés possibles
    const id =
      payload.nameid ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
      payload.sub ||
      payload.id ||
      '';

    // Extraire le rôle depuis plusieurs clés possibles
    const role = 
      payload.role || 
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 
      'CITOYEN';

    // Préparer l'URL de la photo si elle existe et est relative
    let photoUrl = payload.photoUrl || payload['photoUrl'] || null;
    if (photoUrl && !photoUrl.startsWith('http') && !photoUrl.startsWith('data:')) {
      const baseUrl = environment.apiUrl.replace('/api', '');
      photoUrl = `${baseUrl}${photoUrl.startsWith('/') ? '' : '/'}${photoUrl}`;
    }

    return {
      id,
      email: payload.email || '',
      nom: payload.nom || '',
      prenom: payload.prenom || '',
      region: payload.region || '',
      delegation: payload.delegation || '',
      cin: payload.cin || '',
      telephone: payload.telephone || payload['telephone'] || '',
      role: role,
      photoUrl: photoUrl,
    };
  }

  // Méthode pour rafraîchir les données utilisateur depuis l'API
  async refreshCurrentUser(): Promise<User | null> {
    try {
      const user = await this.fetchMe().toPromise();
      if (user) {
        // Préparer l'URL de la photo si elle est relative
        if (user.photoUrl && !user.photoUrl.startsWith('http') && !user.photoUrl.startsWith('data:')) {
          const baseUrl = environment.apiUrl.replace('/api', '');
          user.photoUrl = `${baseUrl}${user.photoUrl.startsWith('/') ? '' : '/'}${user.photoUrl}`;
        }
        // Mettre à jour les données utilisateur dans le token ou localStorage
        localStorage.setItem('current_user', JSON.stringify(user));
        return user;
      }
    } catch (error) {
      console.error('Erreur lors du rafraîchissement des données utilisateur:', error);
    }
    return null;
  }

  // Méthode pour obtenir l'utilisateur depuis le localStorage (plus à jour)
  getCurrentUserFromStorage(): User | null {
    try {
      const storedUser = localStorage.getItem('current_user');
      if (storedUser) {
        return JSON.parse(storedUser);
      }
    } catch (error) {
      console.error('Erreur lors de la lecture des données utilisateur:', error);
    }
    return this.getCurrentUser(); // Fallback vers le token
  }

  fetchMe(): Observable<User> {
    return this.apiService.get<User>('/auth/me').pipe(
      tap(user => {
        // Préparer l'URL de la photo si elle est relative
        if (user.photoUrl && !user.photoUrl.startsWith('http') && !user.photoUrl.startsWith('data:')) {
          const baseUrl = environment.apiUrl.replace('/api', '');
          user.photoUrl = `${baseUrl}${user.photoUrl.startsWith('/') ? '' : '/'}${user.photoUrl}`;
        }
      })
    );
  }

  getUserRole(): string {
    return this.getCurrentUser()?.role || 'CITOYEN';
  }

  getHomeRouteByRole(role?: string): string {
    const normalized = this.normalizeRole(role ?? this.getUserRole());

    if (normalized === 'SUPERADMIN' || normalized === 'ADMIN') {
      return '/admin';
    }

    if (this.isBureauRole(normalized)) {
      return '/dashboard-bureau-et-service';
    }

    if (this.isServiceRole(normalized)) {
      return '/service/dashboard';
    }

    if (this.isOrganisationRole(normalized)) {
      return '/organisation-dashboard';
    }

    return '/dashboard';
  }

  normalizeRole(role: string | null | undefined): string {
    return (role ?? '')
      .toUpperCase()
      .replace(/[\s_-]+/g, '');
  }

  isBureauRole(role: string | null | undefined): boolean {
    const normalized = this.normalizeRole(role);
    return normalized === 'BUREAUDECONTROLE';
  }

  isServiceRole(role: string | null | undefined): boolean {
    const normalized = this.normalizeRole(role);
    return normalized === 'SERVICE';
  }

  isOrganisationRole(role: string | null | undefined): boolean {
    const normalized = this.normalizeRole(role);
    return normalized === 'ORGANISATION';
  }
}
