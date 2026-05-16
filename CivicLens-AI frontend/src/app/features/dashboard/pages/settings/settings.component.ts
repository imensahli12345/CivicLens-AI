import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../../core/services/auth.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  template: `
    <div class="settings-page fade-in">
      <div class="page-topbar">
        <a routerLink="/dashboard" class="btn-back">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="15 18 9 12 15 6" />
          </svg>
          Retour
        </a>
      </div>

      <div class="page-header">
        <div>
          <h1 class="page-title">Paramètres</h1>
          <p class="page-subtitle">Gérez vos préférences et votre compte</p>
        </div>
      </div>

      <div class="section">
        <div class="section-title">APPLICATION</div>

        <div class="row">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="m5 8 6 6"/><path d="m4 14 6-6 2-3"/><path d="M2 5h12"/><path d="M7 2h1"/><path d="m22 22-5-10-5 10"/><path d="M14 18h6"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Langue</div>
          </div>
          <select class="row-select" [(ngModel)]="language" disabled>
            <option value="fr">Français</option>
          </select>
        </div>

        <div class="row">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Thème</div>
          </div>
          <select class="row-select" [(ngModel)]="themeMode" (ngModelChange)="onThemeChange($event)">
            <option value="auto">Automatique</option>
            <option value="light">Clair</option>
            <option value="dark">Sombre</option>
          </select>
        </div>
      </div>

      <div class="section">
        <div class="section-title">PROFIL</div>

        <a routerLink="/dashboard/profil/modifier" class="row row-link">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Profil</div>
            <div class="row-sub">Modifier vos informations</div>
          </div>
          <div class="row-chevron">›</div>
        </a>
      </div>

      <div class="section">
        <div class="section-title">CONFIDENTIALITÉ & SÉCURITÉ</div>

        <a routerLink="/dashboard/profil/modifier" class="row row-link">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Changer le mot de passe</div>
          </div>
          <div class="row-chevron">›</div>
        </a>

        <div class="row">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Authentification à deux facteurs</div>
            <div class="row-sub">Via SMS ou application (bientôt)</div>
          </div>
          <label class="toggle disabled">
            <input type="checkbox" disabled />
            <span class="track"><span class="thumb"></span></span>
          </label>
        </div>
      </div>

      <div class="section">
        <div class="section-title">SUPPORT</div>

        <button type="button" class="row row-link" (click)="toggleReclamation()">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a4 4 0 0 1-4 4H7l-4 3V7a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4z"/><path d="M8 9h8"/><path d="M8 13h6"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Réclamation sur l’application</div>
            <div class="row-sub">Envoyer un message au support</div>
          </div>
          <div class="row-chevron" [class.open]="reclamationOpen">›</div>
        </button>

        <div *ngIf="reclamationOpen" class="reclamation-card">
          <div *ngIf="reclamationSent" class="reclamation-banner success">Réclamation envoyée.</div>
          <div *ngIf="reclamationError" class="reclamation-banner error">{{ reclamationError }}</div>

          <div class="reclamation-fields">
            <div class="field">
              <label class="field-label" for="reclamationEmail">Email <span class="required">*</span></label>
              <input
                id="reclamationEmail"
                class="field-input"
                type="email"
                [(ngModel)]="reclamationEmail"
                [disabled]="reclamationSending"
                placeholder="votre@email.com"
                autocomplete="email"
              />
            </div>

            <div class="field">
              <label class="field-label" for="reclamationSubject">Sujet</label>
              <input
                id="reclamationSubject"
                class="field-input"
                type="text"
                [(ngModel)]="reclamationSubject"
                [disabled]="reclamationSending"
                placeholder="Ex: Problème de connexion"
                maxlength="120"
              />
            </div>

            <div class="field">
              <label class="field-label" for="reclamationMessage">Message <span class="required">*</span></label>
              <textarea
                id="reclamationMessage"
                class="field-textarea"
                [(ngModel)]="reclamationMessage"
                [disabled]="reclamationSending"
                placeholder="Décrivez le problème (min 10 caractères)..."
                rows="4"
              ></textarea>
            </div>
          </div>

          <div class="reclamation-actions">
            <button type="button" class="btn btn-primary" (click)="sendReclamation()" [disabled]="reclamationSending">
              <span class="spinner tiny" *ngIf="reclamationSending"></span>
              <span *ngIf="!reclamationSending">Envoyer</span>
            </button>
          </div>
        </div>
      </div>

      <div class="section">
        <div class="section-title">VERSION</div>
        <div class="row">
          <div class="row-icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Version de l’application</div>
            <div class="row-sub">{{ appVersion }}</div>
          </div>
          <span class="pill">À jour</span>
        </div>
      </div>

      <div class="section">
        <div class="section-title">COMPTE</div>

        <button type="button" class="row row-danger" (click)="logout()">
          <div class="row-icon danger">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 17l5-5-5-5"/><path d="M15 12H3"/><path d="M21 12a9 9 0 1 0-9 9"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Se déconnecter</div>
          </div>
        </button>

        <button type="button" class="row row-danger" (click)="openDeleteModal()">
          <div class="row-icon danger">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18"/><path d="M8 6V4h8v2"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M10 11v6"/><path d="M14 11v6"/></svg>
          </div>
          <div class="row-body">
            <div class="row-title">Supprimer mon compte</div>
            <div class="row-sub">Suppression définitive des données</div>
          </div>
        </button>
      </div>
    </div>

    <div *ngIf="deleteModalOpen" class="modal-overlay" (click)="closeDeleteModal()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-head">
          <div class="modal-icon danger">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.3" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/></svg>
          </div>
          <div class="modal-title-wrap">
            <div class="modal-title">Supprimer votre compte ?</div>
            <div class="modal-subtitle">Cette action est définitive.</div>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-ghost" (click)="closeDeleteModal()" [disabled]="deleting">Annuler</button>
          <button class="btn btn-danger" (click)="confirmDelete()" [disabled]="deleting">
            <span class="spinner tiny" *ngIf="deleting"></span>
            <span *ngIf="!deleting">Supprimer</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .settings-page {
        max-width: 860px;
        margin: 0 auto;
      }

      .page-topbar {
        display: flex;
        align-items: center;
        margin-bottom: 1.25rem;
      }

      .btn-back {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        font-weight: 600;
        color: var(--text-primary);
        text-decoration: none;
        padding: 0.35rem 0.6rem;
        border-radius: 10px;
        background: color-mix(in srgb, var(--bg-surface) 80%, transparent);
        border: 1px solid var(--border);
      }

      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-end;
        gap: 1rem;
        margin-bottom: 1.5rem;
      }

      .page-title {
        margin: 0;
        font-size: 1.9rem;
        letter-spacing: -0.02em;
        color: var(--text-primary);
      }

      .page-subtitle {
        margin: 0.25rem 0 0;
        color: var(--text-muted);
      }

      .section {
        margin-top: 1.25rem;
      }

      .section-title {
        font-size: 0.78rem;
        font-weight: 900;
        letter-spacing: 0.06em;
        color: var(--text-muted);
        padding: 0 0.2rem 0.6rem;
      }

      .row {
        display: flex;
        align-items: center;
        gap: 0.9rem;
        padding: 0.95rem 1rem;
        background: var(--bg-surface);
        border: 1px solid var(--border);
        border-radius: 14px;
        width: 100%;
        text-align: left;
      }

      .row + .row {
        margin-top: 0.7rem;
      }

      .row-link {
        text-decoration: none;
        cursor: pointer;
      }

      .row:hover {
        box-shadow: var(--shadow-md);
        transform: translateY(-1px);
        transition: transform 0.18s ease, box-shadow 0.18s ease;
      }

      button.row {
        cursor: pointer;
        border: 1px solid var(--border);
      }

      .row-icon {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: color-mix(in srgb, var(--accent) 14%, transparent);
        color: var(--accent);
        flex-shrink: 0;
      }

      .row-icon.danger {
        background: color-mix(in srgb, var(--danger) 14%, transparent);
        color: var(--danger);
      }

      .row-body {
        flex: 1;
        min-width: 0;
      }

      .row-title {
        font-weight: 900;
        color: var(--text-primary);
      }

      .row-sub {
        color: var(--text-muted);
        font-size: 0.9rem;
        margin-top: 0.15rem;
      }

      .row-select {
        border: 1px solid var(--border);
        background: var(--bg-muted);
        color: var(--text-primary);
        border-radius: 12px;
        padding: 0.55rem 0.75rem;
        font-weight: 800;
        outline: none;
      }

      .row-chevron {
        color: var(--text-muted);
        font-size: 1.4rem;
        padding: 0 0.2rem;
        transition: transform 0.18s ease;
      }

      .row-chevron.open {
        transform: rotate(90deg);
      }

      .row-danger .row-title {
        color: var(--danger);
      }

      .pill {
        padding: 0.25rem 0.6rem;
        border-radius: 999px;
        background: color-mix(in srgb, var(--success) 16%, transparent);
        color: var(--success);
        font-weight: 900;
        font-size: 0.78rem;
        border: 1px solid color-mix(in srgb, var(--success) 28%, transparent);
      }

      .toggle {
        display: inline-flex;
        align-items: center;
      }

      .toggle input {
        display: none;
      }

      .toggle .track {
        width: 46px;
        height: 26px;
        border-radius: 999px;
        border: 1px solid var(--border);
        background: var(--bg-muted);
        display: inline-flex;
        align-items: center;
        padding: 2px;
      }

      .toggle .thumb {
        width: 22px;
        height: 22px;
        border-radius: 999px;
        background: var(--bg-surface);
        border: 1px solid var(--border);
      }

      .toggle.disabled {
        opacity: 0.6;
      }

      .modal-overlay {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.45);
        backdrop-filter: blur(6px);
        -webkit-backdrop-filter: blur(6px);
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 1.25rem;
        z-index: 80;
      }

      .modal-card {
        width: min(520px, 100%);
        background: var(--bg-surface);
        border: 1px solid var(--border);
        border-radius: 16px;
        box-shadow: 0 18px 40px rgba(0,0,0,0.18);
        overflow: hidden;
      }

      .modal-head {
        display: flex;
        align-items: flex-start;
        gap: 0.9rem;
        padding: 1.25rem 1.25rem 0.9rem 1.25rem;
      }

      .modal-icon {
        width: 40px;
        height: 40px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        border: 1px solid transparent;
      }

      .modal-icon.danger {
        background: color-mix(in srgb, var(--danger) 14%, transparent);
        color: var(--danger);
        border-color: color-mix(in srgb, var(--danger) 28%, transparent);
      }

      .modal-title {
        font-weight: 900;
        font-size: 1.1rem;
        color: var(--text-primary);
        letter-spacing: -0.01em;
      }

      .modal-subtitle {
        margin-top: 0.25rem;
        color: var(--text-muted);
        font-size: 0.95rem;
      }

      .modal-actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        padding: 0.9rem 1.25rem 1.25rem 1.25rem;
      }

      .spinner.tiny {
        width: 14px;
        height: 14px;
        border-radius: 50%;
        border: 2px solid color-mix(in srgb, currentColor 25%, transparent);
        border-top-color: currentColor;
        animation: spin 1s linear infinite;
      }

      .reclamation-card {
        background: var(--bg-surface);
        border: 1px solid var(--border);
        border-radius: 14px;
        padding: 1rem;
      }

      .reclamation-banner {
        padding: 0.75rem 0.9rem;
        border-radius: 12px;
        font-weight: 800;
        margin-bottom: 0.85rem;
        border: 1px solid transparent;
      }

      .reclamation-banner.success {
        background: color-mix(in srgb, var(--success) 14%, transparent);
        color: var(--success);
        border-color: color-mix(in srgb, var(--success) 28%, transparent);
      }

      .reclamation-banner.error {
        background: color-mix(in srgb, var(--danger) 14%, transparent);
        color: var(--danger);
        border-color: color-mix(in srgb, var(--danger) 28%, transparent);
      }

      .reclamation-fields {
        display: grid;
        gap: 0.85rem;
      }

      .field-label {
        display: block;
        font-weight: 900;
        color: var(--text-primary);
        margin-bottom: 0.35rem;
        letter-spacing: -0.01em;
      }

      .required {
        color: var(--danger);
        margin-left: 2px;
      }

      .field-input,
      .field-textarea {
        width: 100%;
        border: 1px solid var(--border);
        background: var(--bg-muted);
        color: var(--text-primary);
        border-radius: 12px;
        padding: 0.7rem 0.8rem;
        outline: none;
        font-weight: 700;
      }

      .field-textarea {
        resize: vertical;
        min-height: 110px;
        font-weight: 650;
      }

      .reclamation-actions {
        display: flex;
        justify-content: flex-end;
        margin-top: 0.9rem;
      }

      @keyframes spin {
        to { transform: rotate(360deg); }
      }

      @media (max-width: 900px) {
        .settings-page { max-width: none; }
      }
    `
  ]
})
export class SettingsComponent implements OnInit {
  appVersion = environment.appVersion ?? '—';
  language: 'fr' = 'fr';
  themeMode: 'auto' | 'light' | 'dark' = 'auto';
  deleteModalOpen = false;
  deleting = false;
  reclamationEmail = '';
  reclamationSubject = '';
  reclamationMessage = '';
  reclamationSending = false;
  reclamationSent = false;
  reclamationError: string | null = null;
  reclamationOpen = false;
  private mediaQuery: MediaQueryList | null = null;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    const raw = (localStorage.getItem('civiclens-theme') ?? 'auto').toLowerCase();
    this.themeMode = (raw === 'dark' || raw === 'light' || raw === 'auto') ? (raw as any) : 'auto';
    this.applyTheme();

    const user = this.authService.getCurrentUserFromStorage();
    this.reclamationEmail = user?.email ?? '';

    if (window.matchMedia) {
      this.mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      const handler = () => {
        if (this.themeMode === 'auto') this.applyTheme();
      };
      if ('addEventListener' in this.mediaQuery) this.mediaQuery.addEventListener('change', handler);
      else (this.mediaQuery as any).addListener(handler);
    }
  }

  onThemeChange(value: 'auto' | 'light' | 'dark'): void {
    this.themeMode = value;
    localStorage.setItem('civiclens-theme', value);
    this.applyTheme();
  }

  private applyTheme(): void {
    const shouldDark =
      this.themeMode === 'dark'
        ? true
        : this.themeMode === 'light'
          ? false
          : this.mediaQuery?.matches ?? (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches);
    document.documentElement.classList.toggle('theme-dark', !!shouldDark);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  openDeleteModal(): void {
    this.deleteModalOpen = true;
  }

  closeDeleteModal(): void {
    if (this.deleting) return;
    this.deleteModalOpen = false;
  }

  confirmDelete(): void {
    if (this.deleting) return;
    this.deleting = true;
    this.authService.deleteMe().subscribe({
      next: () => {
        this.deleting = false;
        this.closeDeleteModal();
        this.logout();
      },
      error: () => {
        this.deleting = false;
        this.closeDeleteModal();
      }
    });
  }

  sendReclamation(): void {
    if (this.reclamationSending) return;

    const email = (this.reclamationEmail ?? '').trim();
    const subject = (this.reclamationSubject ?? '').trim();
    const message = (this.reclamationMessage ?? '').trim();

    this.reclamationSent = false;
    this.reclamationError = null;

    if (!email) {
      this.reclamationError = 'Email requis.';
      return;
    }

    if (!message || message.length < 10) {
      this.reclamationError = 'Message requis (min 10 caractères).';
      return;
    }

    this.reclamationSending = true;
    this.authService.sendReclamation({
      email,
      subject,
      message,
      appVersion: this.appVersion,
      context: 'settings'
    }).subscribe({
      next: () => {
        this.reclamationSending = false;
        this.reclamationSent = true;
        this.reclamationMessage = '';
      },
      error: (err: any) => {
        this.reclamationSending = false;
        this.reclamationError = err?.message || 'Erreur lors de l’envoi de la réclamation.';
      }
    });
  }

  toggleReclamation(): void {
    this.reclamationOpen = !this.reclamationOpen;
  }
}
