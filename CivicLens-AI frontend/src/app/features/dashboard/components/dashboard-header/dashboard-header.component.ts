import { Component, HostListener, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { User } from '../../../../shared/models/user.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { Notification } from '../../../../shared/models/notification.model';
import { SidebarLink } from '../../config/dashboard.config';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-dashboard-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './dashboard-header.component.html',
  styleUrls: ['./dashboard-header.component.css']
})
export class DashboardHeaderComponent implements OnInit {
  @Input() user: User | null = null;
  @Input() links: SidebarLink[] = [];

  notificationsOpen = signal(false);
  profileMenuOpen = signal(false);
  mobileNavOpen = signal(false);
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
    private authService: AuthService,
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
      this.profileMenuOpen.set(false);
      this.mobileNavOpen.set(false);
    }
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
    this.profileMenuOpen.set(false);
    if (n.relatedEvenementId) {
      this.router.navigate(['/dashboard/evenements', n.relatedEvenementId]);
      return;
    }
    if (n.relatedSignalementId != null) {
      this.router.navigate(['/dashboard/signalement', n.relatedSignalementId]);
      return;
    }
    this.router.navigate(['/dashboard/notifications']);
  }

  toggleProfileMenu(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    const open = !this.profileMenuOpen();
    this.profileMenuOpen.set(open);
    if (open) {
      this.notificationsOpen.set(false);
      this.mobileNavOpen.set(false);
    }
  }

  toggleMobileNav(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    const open = !this.mobileNavOpen();
    this.mobileNavOpen.set(open);
    if (open) {
      this.notificationsOpen.set(false);
      this.profileMenuOpen.set(false);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  @HostListener('document:click')
  closeMenus(): void {
    if (this.notificationsOpen()) this.notificationsOpen.set(false);
    if (this.profileMenuOpen()) this.profileMenuOpen.set(false);
    if (this.mobileNavOpen()) this.mobileNavOpen.set(false);
  }

  getIcon(name: string): string {
    const icons: Record<string, string> = {
      grid: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/></svg>`,
      list: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><line x1="8" y1="6" x2="21" y2="6"/><line x1="8" y1="12" x2="21" y2="12"/><line x1="8" y1="18" x2="21" y2="18"/><line x1="3" y1="6" x2="3.01" y2="6"/><line x1="3" y1="12" x2="3.01" y2="12"/><line x1="3" y1="18" x2="3.01" y2="18"/></svg>`,
      'plus-circle': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="16"/><line x1="8" y1="12" x2="16" y2="12"/></svg>`,
      'map-pin': `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"/><circle cx="12" cy="10" r="3"/></svg>`,
      calendar: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>`,
      user: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>`,
      settings: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 1v2"/><path d="M12 21v2"/><path d="M4.22 4.22l1.42 1.42"/><path d="M18.36 18.36l1.42 1.42"/><path d="M1 12h2"/><path d="M21 12h2"/><path d="M4.22 19.78l1.42-1.42"/><path d="M18.36 5.64l1.42-1.42"/><circle cx="12" cy="12" r="4"/></svg>`,
      info: `<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>`
    };
    return icons[name] || icons['grid'];
  }
}
