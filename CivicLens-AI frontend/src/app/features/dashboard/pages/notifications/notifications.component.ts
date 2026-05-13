import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Notification } from '../../../../shared/models/notification.model';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.css']
})
export class NotificationsComponent implements OnInit {
  notifications = signal<Notification[]>([]);
  loading = signal(true);
  errorMsg = signal<string | null>(null);

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMsg.set(null);

    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notificationService.getMyNotifications(20).subscribe({
          next: (list) => {
            this.notifications.set(list);
            this.loading.set(false);
          },
          error: (err) => {
            const msg =
              err?.error && typeof err.error === 'string' && err.error.trim()
                ? err.error
                : 'Impossible de charger les notifications.';
            this.errorMsg.set(msg);
            this.loading.set(false);
          }
        });
      },
      error: () => {
        // If mark-all fails, still try to load the list
        this.notificationService.getMyNotifications(20).subscribe({
          next: (list) => {
            this.notifications.set(list);
            this.loading.set(false);
          },
          error: (err) => {
            const msg =
              err?.error && typeof err.error === 'string' && err.error.trim()
                ? err.error
                : 'Impossible de charger les notifications.';
            this.errorMsg.set(msg);
            this.loading.set(false);
          }
        });
      }
    });
  }

  formatShortDate(d: Date): string {
    if (!d) return '';
    return new Date(d).toLocaleString('fr-FR', {
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  canOpen(n: Notification): boolean {
    return !!n.relatedEvenementId || n.relatedSignalementId != null;
  }

  open(n: Notification): void {
    if (n.relatedEvenementId) {
      this.router.navigate(['/dashboard/evenements', n.relatedEvenementId]);
      return;
    }
    if (n.relatedSignalementId != null) {
      this.router.navigate(['/dashboard/signalement', n.relatedSignalementId]);
    }
  }
}

