import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NotificationService } from '../../../../core/services/notification.service';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ReactiveFormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  unreadCount = signal(0);
  passwordOpen = signal(false);
  passwordForm!: FormGroup;
  passwordLoading = signal(false);
  passwordError = signal<string | null>(null);
  passwordSuccess = signal<string | null>(null);
  private unreadTimer: any;

  constructor(
    private router: Router,
    private notificationService: NotificationService,
    private fb: FormBuilder,
    private adminService: AdminService
  ) {}

  ngOnInit(): void {
    this.passwordForm = this.fb.group({
      ancienMotDePasse: ['', [Validators.required]],
      nouveauMotDePasse: ['', [Validators.required, Validators.minLength(6)]],
      confirmNouveauMotDePasse: ['', [Validators.required]]
    });

    this.refreshUnreadCount();
    this.unreadTimer = setInterval(() => this.refreshUnreadCount(), 15000);
  }

  ngOnDestroy(): void {
    if (this.unreadTimer) clearInterval(this.unreadTimer);
  }

  refreshUnreadCount(): void {
    this.notificationService.getMyUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count ?? 0),
      error: () => this.unreadCount.set(0)
    });
  }

  openPasswordModal(): void {
    this.passwordError.set(null);
    this.passwordSuccess.set(null);
    this.passwordForm.reset();
    this.passwordOpen.set(true);
  }

  closePasswordModal(): void {
    if (this.passwordLoading()) return;
    this.passwordOpen.set(false);
  }

  submitPassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const ancienMotDePasse = String(this.passwordForm.value.ancienMotDePasse ?? '');
    const nouveauMotDePasse = String(this.passwordForm.value.nouveauMotDePasse ?? '');
    const confirm = String(this.passwordForm.value.confirmNouveauMotDePasse ?? '');

    if (nouveauMotDePasse !== confirm) {
      this.passwordError.set('Les mots de passe ne correspondent pas.');
      return;
    }

    this.passwordLoading.set(true);
    this.passwordError.set(null);
    this.passwordSuccess.set(null);

    this.adminService.changeMyPassword({ ancienMotDePasse, nouveauMotDePasse }).subscribe({
      next: (res) => {
        this.passwordLoading.set(false);
        this.passwordSuccess.set(res?.message || 'Mot de passe mis à jour.');
        setTimeout(() => this.closePasswordModal(), 800);
      },
      error: (err: any) => {
        this.passwordLoading.set(false);
        this.passwordError.set(err?.message || 'Impossible de changer le mot de passe.');
      }
    });
  }

  logout(): void {
    localStorage.clear();
    this.router.navigate(['/login']);
  }
}
