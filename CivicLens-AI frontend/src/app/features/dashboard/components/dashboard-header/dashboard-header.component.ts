import { Component, Input, Output, EventEmitter, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { User } from '../../../../shared/models/user.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { Notification } from '../../../../shared/models/notification.model';

@Component({
  selector: 'app-dashboard-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-header.component.html',
  styleUrls: ['./dashboard-header.component.css']
})
export class DashboardHeaderComponent implements OnInit {
  @Input() user: User | null = null;
  @Output() toggleSidebar = new EventEmitter<void>();

  notificationsOpen = signal(false);
  topNotifications = signal<Notification[]>([]);
  unreadCount = signal(0);
  loadingTop = signal(false);

  getGreeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Bonjour';
    if (h < 18) return 'Bon après-midi';
    return 'Bonsoir';
  }

  getDate(): string {
    return new Date().toLocaleDateString('fr-TN', {
      weekday: 'long', day: 'numeric', month: 'long'
    });
  }

  getAvatarLetters(): string {
    const nom = this.user?.nom || '';
    const prenom = this.user?.prenom || '';
    return `${nom.charAt(0)}${prenom.charAt(0)}`.toUpperCase() || 'CL';
  }

  constructor(
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.notificationService.getMyUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count),
      error: () => this.unreadCount.set(0)
    });
  }

  toggleNotifications(): void {
    const open = !this.notificationsOpen();
    this.notificationsOpen.set(open);
    if (open) {
      // UX simple : quand l'utilisateur ouvre, on marque tout comme lu puis on recharge les 3 dernières.
      this.notificationService.markAllAsRead().subscribe({
        next: () => {
          this.unreadCount.set(0);
          this.loadTopNotifications();
        },
        error: () => {
          // Si mark-all échoue, on recharge quand même la liste
          this.loadTopNotifications();
        }
      });
    }
  }

  private loadTopNotifications(): void {
    this.loadingTop.set(true);
    this.notificationService.getMyNotifications(3).subscribe({
      next: (list) => {
        this.topNotifications.set(list);
        this.loadingTop.set(false);
      },
      error: () => {
        this.topNotifications.set([]);
        this.loadingTop.set(false);
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

  onNotificationClick(n: Notification): void {
    this.notificationService.markAsRead(n.id).subscribe({
      error: () => { /* ignore */ }
    });

    this.topNotifications.update(list =>
      list.map(x => x.id === n.id ? { ...x, isRead: true } : x)
    );
    this.unreadCount.update(c => Math.max(0, c - 1));

    this.notificationsOpen.set(false);
    if (n.relatedEvenementId) {
      this.router.navigate(['/dashboard/evenements', n.relatedEvenementId]);
    }
  }
}