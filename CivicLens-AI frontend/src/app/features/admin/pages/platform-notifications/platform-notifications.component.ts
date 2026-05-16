import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../../../core/services/notification.service';
import { Notification } from '../../../../shared/models/notification.model';

@Component({
  selector: 'app-platform-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './platform-notifications.component.html',
  styleUrl: './platform-notifications.component.css'
})
export class PlatformNotificationsComponent implements OnInit {
  loading = signal(true);
  error = signal<string | null>(null);
  notifications = signal<Notification[]>([]);

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.notificationService.markAllAsRead().subscribe({
      next: () => this.fetchList(),
      error: () => this.fetchList()
    });
  }

  private fetchList(): void {
    this.notificationService.getMyAllNotifications().subscribe({
      next: (list) => {
        const filtered = (list ?? []).filter((n) =>
          n.type === 'PLATFORM_RECLAMATION' ||
          n.type === 'ACTIVATION_REQUEST' ||
          n.type === 'ACTIVATION_REQUEST_SERVICE'
        );
        this.notifications.set(filtered);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.error.set(err?.message || 'Impossible de charger les notifications.');
        this.notifications.set([]);
        this.loading.set(false);
      }
    });
  }

  formatDate(value: any): string {
    if (!value) return '';
    try {
      return new Date(value).toLocaleString('fr-FR', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return String(value);
    }
  }

  open(n: Notification): void {
    if (n.type === 'PLATFORM_RECLAMATION') {
      this.router.navigate(['/admin/reclamations']);
      return;
    }
    if (n.type === 'ACTIVATION_REQUEST' || n.type === 'ACTIVATION_REQUEST_SERVICE') {
      this.router.navigate(['/admin/activation-requests']);
      return;
    }
    this.router.navigate(['/admin/dashboard']);
  }
}
