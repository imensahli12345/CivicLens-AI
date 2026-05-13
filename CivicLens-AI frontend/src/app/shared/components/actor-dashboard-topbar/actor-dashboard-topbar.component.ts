import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-actor-dashboard-topbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './actor-dashboard-topbar.component.html',
  styleUrl: './actor-dashboard-topbar.component.css'
})
export class ActorDashboardTopbarComponent {
  @Input() userName = 'CivicLens User';
  @Input() subtitle = 'Dashboard Bureau et Service';
  @Input() homeRoute = '/dashboard-bureau-et-service';
  @Input() showServicesMenu = true;
  @Input() showAssignSignalementAction = true;
  @Input() showProfile = true;
  @Input() showCarteSignalements = false;

  @Output() serviceAction = new EventEmitter<'view' | 'activate' | 'deactivate'>();
  @Output() signalementAction = new EventEmitter<'view' | 'assign'>();
  @Output() profileClick = new EventEmitter<void>();

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  onServiceAction(value: 'view' | 'activate' | 'deactivate'): void {
    this.serviceAction.emit(value);
  }

  onSignalementAction(value: 'view' | 'assign'): void {
    this.signalementAction.emit(value);
  }

  onProfileClick(): void {
    this.profileClick.emit();
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  get initials(): string {
    const cleaned = this.userName.trim();
    if (!cleaned) return 'CU';
    const parts = cleaned.split(/\s+/).slice(0, 2);
    return parts.map((part) => part[0]?.toUpperCase() ?? '').join('') || 'CU';
  }
}
