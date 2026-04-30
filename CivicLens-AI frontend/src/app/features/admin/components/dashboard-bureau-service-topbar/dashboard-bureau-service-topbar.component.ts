import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard-bureau-service-topbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-bureau-service-topbar.component.html',
  styleUrl: './dashboard-bureau-service-topbar.component.css'
})
export class DashboardBureauServiceTopbarComponent {
  @Input() userName = 'CivicLens User';

  @Output() serviceAction = new EventEmitter<'view' | 'activate' | 'deactivate'>();
  @Output() signalementAction = new EventEmitter<'view' | 'assign'>();
  @Output() profileClick = new EventEmitter<void>();

  onServiceAction(value: 'view' | 'activate' | 'deactivate'): void {
    this.serviceAction.emit(value);
  }

  onSignalementAction(value: 'view' | 'assign'): void {
    this.signalementAction.emit(value);
  }

  onProfileClick(): void {
    this.profileClick.emit();
  }

  get initials(): string {
    const cleaned = this.userName.trim();
    if (!cleaned) return 'CU';
    const parts = cleaned.split(/\s+/).slice(0, 2);
    return parts.map((part) => part[0]?.toUpperCase() ?? '').join('') || 'CU';
  }
}
