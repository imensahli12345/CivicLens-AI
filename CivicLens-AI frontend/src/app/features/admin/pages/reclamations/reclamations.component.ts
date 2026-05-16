import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../services/admin.service';

type ReclamationStatus = 'TOUTES' | 'NOUVELLE' | 'TRAITEE';

@Component({
  selector: 'app-admin-reclamations',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reclamations.component.html',
  styleUrl: './reclamations.component.css'
})
export class ReclamationsComponent implements OnInit {
  loading = true;
  error = '';
  items: any[] = [];
  selected: any | null = null;

  status: ReclamationStatus = 'TOUTES';
  search = '';
  updatingId: string | null = null;

  constructor(private admin: AdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    const status = this.status === 'TOUTES' ? undefined : this.status;
    this.admin.getPlatformReclamations({ status }).subscribe({
      next: (list) => {
        this.items = Array.isArray(list) ? list : [];
        this.loading = false;
      },
      error: (err: any) => {
        this.error = err?.message || 'Impossible de charger les réclamations.';
        this.loading = false;
      }
    });
  }

  open(item: any): void {
    this.selected = item;
  }

  close(): void {
    this.selected = null;
  }

  filtered(): any[] {
    const q = this.search.trim().toLowerCase();
    if (!q) return this.items;
    return this.items.filter((r) => {
      const subject = String(r?.subject ?? '').toLowerCase();
      const from = String(r?.fromEmail ?? '').toLowerCase();
      const msg = String(r?.message ?? '').toLowerCase();
      return subject.includes(q) || from.includes(q) || msg.includes(q);
    });
  }

  markTreated(item: any): void {
    const id = String(item?.id ?? '');
    if (!id) return;
    this.updatingId = id;
    this.admin.updateReclamationStatus(id, 'TRAITEE').subscribe({
      next: (res) => {
        item.status = res?.status ?? 'TRAITEE';
        if (this.selected?.id === id) this.selected = { ...this.selected, status: item.status };
        this.updatingId = null;
      },
      error: () => {
        this.updatingId = null;
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
}

