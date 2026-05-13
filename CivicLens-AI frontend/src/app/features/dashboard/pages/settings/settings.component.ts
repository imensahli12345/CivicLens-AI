import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, RouterLink],
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

      <div class="settings-grid">
        <a routerLink="/dashboard/profil" class="settings-card card">
          <div class="settings-card__icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
          </div>
          <div class="settings-card__body">
            <div class="settings-card__title">Profil</div>
            <div class="settings-card__desc">Modifier vos informations personnelles</div>
          </div>
        </a>

        <a routerLink="/dashboard/notifications" class="settings-card card">
          <div class="settings-card__icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
              <path d="M13.73 21a2 2 0 0 1-3.46 0" />
            </svg>
          </div>
          <div class="settings-card__body">
            <div class="settings-card__title">Notifications</div>
            <div class="settings-card__desc">Consulter votre historique de notifications</div>
          </div>
        </a>

        <a routerLink="/dashboard/about" class="settings-card card">
          <div class="settings-card__icon">
            <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="16" x2="12" y2="12" />
              <line x1="12" y1="8" x2="12.01" y2="8" />
            </svg>
          </div>
          <div class="settings-card__body">
            <div class="settings-card__title">À propos</div>
            <div class="settings-card__desc">Découvrir CivicLens AI</div>
          </div>
        </a>
      </div>
    </div>
  `,
  styles: [
    `
      .settings-page {
        max-width: 1100px;
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
        color: var(--text, #0f172a);
        text-decoration: none;
        padding: 0.35rem 0.6rem;
        border-radius: 10px;
        background: rgba(255, 255, 255, 0.7);
        border: 1px solid rgba(15, 23, 42, 0.08);
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
        color: var(--text, #0f172a);
      }

      .page-subtitle {
        margin: 0.25rem 0 0;
        color: var(--text-muted, #64748b);
      }

      .settings-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 1rem;
      }

      .settings-card {
        display: flex;
        gap: 0.9rem;
        padding: 1.1rem 1.1rem;
        border-radius: 16px;
        text-decoration: none;
        border: 1px solid rgba(15, 23, 42, 0.08);
        background: rgba(255, 255, 255, 0.8);
        transition: transform 0.18s ease, box-shadow 0.18s ease;
      }

      .settings-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08);
      }

      .settings-card__icon {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(59, 130, 246, 0.12);
        color: #2563eb;
        flex-shrink: 0;
      }

      .settings-card__title {
        font-weight: 800;
        color: var(--text, #0f172a);
        margin-bottom: 0.15rem;
      }

      .settings-card__desc {
        color: var(--text-muted, #64748b);
        font-size: 0.95rem;
        line-height: 1.35;
      }

      @media (max-width: 900px) {
        .settings-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class SettingsComponent {}

