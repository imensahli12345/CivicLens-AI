import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, FormsModule],
  templateUrl: './auth-layout.component.html',
  styleUrls: ['./auth-layout.component.css']
})
export class AuthLayoutComponent {
  branding = environment.branding;
  appVersion = environment.appVersion ?? null;

  reclamationOpen = false;
  reclamationEmail = '';
  reclamationSubject = '';
  reclamationMessage = '';
  reclamationSending = false;
  reclamationSent = false;
  reclamationError: string | null = null;

  constructor(private authService: AuthService) {}

  openReclamation(): void {
    this.reclamationError = null;
    this.reclamationSent = false;
    this.reclamationOpen = true;
  }

  closeReclamation(): void {
    if (this.reclamationSending) return;
    this.reclamationOpen = false;
  }

  sendReclamation(): void {
    if (this.reclamationSending) return;

    const email = (this.reclamationEmail ?? '').trim();
    const subject = (this.reclamationSubject ?? '').trim();
    const message = (this.reclamationMessage ?? '').trim();

    this.reclamationError = null;
    this.reclamationSent = false;

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
      context: 'auth'
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
}
