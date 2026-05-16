import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-activation-requests',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activation-requests.component.html',
  styleUrl: './activation-requests.component.css'
})
export class ActivationRequestsComponent implements OnInit {
  loading = true;
  error = '';
  success = '';
  items: any[] = [];
  approvingId: string | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    this.success = '';
    this.admin.getActivationRequests().subscribe({
      next: (list) => {
        this.items = Array.isArray(list) ? list : [];
        this.loading = false;
      },
      error: (err: any) => {
        this.error = this.getErrorMessage(err, "Impossible de charger les demandes d'activation.");
        this.loading = false;
      }
    });
  }

  approve(item: any): void {
    const id = String(item?.id ?? '');
    if (!id) return;
    if (item?.approved === true) return;
    this.approvingId = id;
    this.error = '';
    this.success = '';
    this.admin.approveActivationRequest(id).subscribe({
      next: (res: any) => {
        item.approved = true;
        item.userStatut = 'ACTIF';
        item.isRead = true;
        this.success = String(res?.message || 'Compte activé.');
        this.approvingId = null;
      },
      error: (err: any) => {
        this.error = this.getErrorMessage(err, "Impossible d'activer ce compte.");
        this.approvingId = null;
      }
    });
  }

  private getErrorMessage(err: any, fallback: string): string {
    const apiError = err?.error;
    if (typeof apiError === 'string' && apiError.trim()) return apiError;
    if (apiError?.message) return String(apiError.message);
    if (err?.message) return String(err.message);
    return fallback;
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
}
