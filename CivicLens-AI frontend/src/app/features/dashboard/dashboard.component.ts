import { Component, OnInit, signal, computed, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { DashboardHeaderComponent } from './components/dashboard-header/dashboard-header.component'
import { AuthService } from '../../core/services/auth.service';
import { User } from '../../shared/models/user.model';
import { getDashboardConfig, DashboardConfig } from './config/dashboard.config';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, DashboardHeaderComponent, CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  user = signal<User | null>(null);
  config = signal<DashboardConfig | null>(null);
  sidebarOpen = signal(true);
  private checkInterval: any;

  // Vérifier si le numéro de téléphone est manquant
  missingPhone = computed(() => {
    const currentUser = this.user();
    return currentUser && !currentUser.telephone;
  });

  // Vérifier si la notification doit être affichée
  showNotification = computed(() => {
    return this.missingPhone() && !this.isNotificationDismissed();
  });

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.loadUserData();
    // Vérifier périodiquement si les données utilisateur ont changé
    this.startUserCheck();
    // Écouter les mises à jour de profil depuis d'autres composants
    window.addEventListener('profile-updated', this.handleProfileUpdate as EventListener);
  }

  ngOnDestroy(): void {
    // Nettoyer l'intervalle lors de la destruction du composant
    if (this.checkInterval) {
      clearInterval(this.checkInterval);
    }
    // Nettoyer l'écouteur d'événement
    window.removeEventListener('profile-updated', this.handleProfileUpdate as EventListener);
  }

  private handleProfileUpdate(event: Event): void {
    const customEvent = event as CustomEvent;
    const updatedUser = customEvent.detail as User;
    this.user.set(updatedUser);
    // Si le téléphone est maintenant renseigné, réinitialiser la notification
    if (updatedUser.telephone) {
      this.resetNotification();
    }
  }

  private loadUserData(): void {
    // Essayer d'abord depuis le localStorage (plus à jour)
    const storedUser = this.authService.getCurrentUserFromStorage();
    this.user.set(storedUser);
    this.config.set(getDashboardConfig(storedUser?.role || 'CITOYEN'));
    
    // Si on n'a pas de données en cache, rafraîchir immédiatement depuis l'API
    if (!localStorage.getItem('current_user')) {
      this.authService.refreshCurrentUser().then(freshUser => {
        if (freshUser) {
          this.user.set(freshUser);
        }
      });
    }
  }

  private startUserCheck(): void {
    // Vérifier toutes les 3 secondes si l'utilisateur a mis à jour son profil
    this.checkInterval = setInterval(async () => {
      const currentUser = this.user();
      
      // Rafraîchir les données depuis l'API
      try {
        const freshUser = await this.authService.refreshCurrentUser();
        if (freshUser) {
          const storedUser = this.user();
          
          // Si l'utilisateur a changé et a maintenant un téléphone, réinitialiser
          if (freshUser.telephone !== storedUser?.telephone && freshUser.telephone) {
            this.user.set(freshUser);
            this.resetNotification();
          } else if (JSON.stringify(freshUser) !== JSON.stringify(storedUser)) {
            // Mettre à jour même si le téléphone n'a pas changé (autres données)
            this.user.set(freshUser);
          }
        }
      } catch (error) {
        console.log('Erreur lors de la vérification du profil:', error);
      }
    }, 30000); // Augmenté à 30 secondes pour une meilleure stabilité et moins de charge serveur
  }

  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  // Rediriger vers la page de modification du profil
  completeProfile(): void {
    this.router.navigate(['/dashboard/profil/modifier']);
  }

  // Fermer la notification
  dismissNotification(): void {
    // Sauvegarder en localStorage que l'utilisateur a dismissé
    localStorage.setItem('phone-notification-dismissed', 'true');
  }

  // Vérifier si la notification a déjà été dismissée
  isNotificationDismissed(): boolean {
    return localStorage.getItem('phone-notification-dismissed') === 'true';
  }

  // Réinitialiser la notification si le profil est mis à jour
  resetNotification(): void {
    localStorage.removeItem('phone-notification-dismissed');
  }

  // Mettre à jour l'utilisateur et vérifier si le profil est complet
  updateUserProfile(updatedUser: User): void {
    this.user.set(updatedUser);
    // Si le téléphone est maintenant renseigné, réinitialiser la notification
    if (updatedUser.telephone) {
      this.resetNotification();
    }
  }

  // Forcer la vérification manuelle (peut être appelée depuis d'autres composants)
  async checkUserProfile(): Promise<void> {
    await this.authService.refreshCurrentUser();
    this.loadUserData();
  }

  // Méthode pour déclencher manuellement la vérification (utile après modification du profil)
  async forceProfileCheck(): Promise<void> {
    const freshUser = await this.authService.refreshCurrentUser();
    if (freshUser) {
      this.user.set(freshUser);
      if (freshUser.telephone) {
        this.resetNotification();
      }
    }
  }
}