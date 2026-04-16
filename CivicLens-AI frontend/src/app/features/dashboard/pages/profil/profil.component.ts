import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { User } from '../../../../shared/models/user.model';

@Component({
  selector: 'app-profil',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './profil.component.html',
  styleUrls: ['./profil.component.css']
})
export class ProfilComponent implements OnInit {
  user = signal<User | null>(null);
  loading = signal(true);

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.authService.fetchMe().subscribe({
      next: (u) => {
        this.user.set(u);
        this.loading.set(false);
      },
      error: () => {
        this.user.set(this.authService.getCurrentUser());
        this.loading.set(false);
      }
    });
  }

  getInitials(): string {
    const u = this.user();
    return `${u?.nom?.charAt(0) || ''}${u?.prenom?.charAt(0) || ''}`.toUpperCase() || 'CL';
  }

  photoUrl(): string | null {
    return this.user()?.photoUrl || null;
  }
}
