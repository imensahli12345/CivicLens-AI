import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-citoyens',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './citoyens.component.html',
  styleUrl: './citoyens.component.css'
})
export class CitoyensComponent implements OnInit {
  citoyens: any[] = [];
  loading = true;
  error = '';

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.adminService.getCitoyens().subscribe({
      next: (data) => { this.citoyens = data; this.loading = false; },
      error: (err) => { this.error = 'Erreur de chargement'; this.loading = false; }
    });
  }

  updateStatut(id: string, statut: string) {
    this.adminService.updateCitoyenStatut(id, statut).subscribe({
      next: () => {
        const c = this.citoyens.find(x => x.id === id);
        if (c) c.statut = statut;
      }
    });
  }
}