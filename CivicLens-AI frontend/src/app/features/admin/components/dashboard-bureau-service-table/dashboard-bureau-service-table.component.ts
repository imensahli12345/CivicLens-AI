import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface DashboardTableColumn {
  key: string;
  label: string;
  type?: 'text' | 'status';
}

@Component({
  selector: 'app-dashboard-bureau-service-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-bureau-service-table.component.html',
  styleUrl: './dashboard-bureau-service-table.component.css'
})
export class DashboardBureauServiceTableComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() columns: DashboardTableColumn[] = [];
  @Input() rows: any[] = [];
  @Input() showActions = false;
  @Input() emptyMessage = 'No data available.';

  @Output() activate = new EventEmitter<any>();
  @Output() deactivate = new EventEmitter<any>();

  asStatusClass(value: string | number): string {
    const status = String(value).toUpperCase();
    if (status === 'ACTIF' || status === 'RESOLU') return 'status-active';
    if (status === 'INACTIF') return 'status-inactive';
    if (status === 'EN_COURS') return 'status-progress';
    if (status === 'EN_ATTENTE') return 'status-pending';
    return 'status-default';
  }

  canActivate(row: any): boolean {
    return String(row?.statut ?? '').toUpperCase() !== 'ACTIF';
  }

  canDeactivate(row: any): boolean {
    return String(row?.statut ?? '').toUpperCase() !== 'INACTIF';
  }

  onActivate(row: any): void {
    this.activate.emit(row);
  }

  onDeactivate(row: any): void {
    this.deactivate.emit(row);
  }
}
