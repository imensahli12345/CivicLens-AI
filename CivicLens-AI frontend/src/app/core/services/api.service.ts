import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  private getHeaders(isFormData = false): HttpHeaders {
    let headersConfig: any = {};
    if (!isFormData) {
      headersConfig['Content-Type'] = 'application/json';
    }
    return new HttpHeaders(headersConfig);
  }

  post<T>(endpoint: string, data: any): Observable<T> {
    const isFormData = data instanceof FormData;
    return this.http.post<T>(`${this.apiUrl}${endpoint}`, data, { headers: this.getHeaders(isFormData) })
      .pipe(timeout(30000), catchError(this.handleError));
  }

  get<T>(endpoint: string): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}${endpoint}`, { headers: this.getHeaders() })
      .pipe(timeout(30000), catchError(this.handleError));
  }

  put<T>(endpoint: string, data: any): Observable<T> {
    return this.http.put<T>(`${this.apiUrl}${endpoint}`, data, { headers: this.getHeaders() })
      .pipe(timeout(30000), catchError(this.handleError));
  }

  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.apiUrl}${endpoint}`, { headers: this.getHeaders() })
      .pipe(timeout(30000), catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMessage = 'Une erreur est survenue';
    if (error.error instanceof ErrorEvent) {
      errorMessage = error.error.message;
    } else {
      if (typeof error.error === 'string' && error.error.trim()) {
        errorMessage = error.error;
        return throwError(() => new Error(errorMessage));
      }
      if (error.status === 400) errorMessage = error.error?.message || 'Données invalides';
      else if (error.status === 401) errorMessage = 'Session expirée. Reconnectez-vous.';
      else if (error.status === 403) errorMessage = 'Accès refusé';
      else if (error.status === 404) errorMessage = 'Ressource introuvable';
      else if (error.status === 409) errorMessage = 'Cet email est déjà utilisé';
      else if (error.status === 500) errorMessage = 'Erreur serveur. Veuillez réessayer plus tard.';
    }
    return throwError(() => new Error(errorMessage));
  }
}