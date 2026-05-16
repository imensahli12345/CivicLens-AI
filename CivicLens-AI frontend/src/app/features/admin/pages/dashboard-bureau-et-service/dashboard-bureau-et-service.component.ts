import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BureauDashboardService } from '../../../../core/services/bureau-dashboard.service';
import { BureauSignalementFilters } from '../../../../core/services/bureau-signalements.service';
import { AboutFooterComponent } from '../../../../shared/components/AboutFooterComponent/about-footer.component';
import { ActorDashboardTopbarComponent } from '../../../../shared/components/actor-dashboard-topbar/actor-dashboard-topbar.component';
import {
  BureauServicesTableComponent,
  ServicesTableConfig
} from '../../../../shared/components/bureau-services-table/bureau-services-table.component';
import { DashboardCalendarComponent } from '../../../../shared/components/dashboard-calendar/dashboard-calendar.component';
import { ProfileFieldConfig, ProfileModalComponent } from '../../../../shared/components/profile-modal/profile-modal.component';
import { BaseModalComponent } from '../../../../shared/components/base-modal/base-modal.component';
import {
  QuickActionItem,
  QuickActionsComponent
} from '../../../../shared/components/quick-actions/quick-actions.component';
import {
  ReclamationsChartComponent,
  ReclamationsStats
} from '../../../../shared/components/reclamations-chart/reclamations-chart.component';
import { SignalementsComponent } from '../../../../shared/components/signalements/signalements.component';
import { SimpleAlertComponent } from '../../../../shared/components/simple-alert/simple-alert.component';

interface HeroSlide {
  src: string;
  alt: string;
}

type QuickFilterType = 'statut' | 'gravite' | 'priorite' | 'date';

@Component({
  selector: 'app-dashboard-bureau-et-service',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ActorDashboardTopbarComponent,
    ReclamationsChartComponent,
    DashboardCalendarComponent,
    QuickActionsComponent,
    SignalementsComponent,
    AboutFooterComponent,
    BureauServicesTableComponent,
    ProfileModalComponent,
    BaseModalComponent,
    SimpleAlertComponent
  ],
  templateUrl: './dashboard-bureau-et-service.component.html',
  styleUrl: './dashboard-bureau-et-service.component.css'
})
export class DashboardBureauEtServiceComponent implements OnInit {
  readonly quickFilterActions: QuickActionItem[] = [
    {
      id: 'filter-status',
      title: 'Filtrer par statut',
      subtitle: 'Voir les signalements selon leur etat actuel',
      icon: 'layers',
      accentColor: '#a78bfa'
    },
    {
      id: 'filter-gravite',
      title: 'Filtrer par gravite',
      subtitle: 'Afficher les signalements selon leur niveau de gravite',
      icon: 'alert',
      accentColor: '#5eead4'
    },
    {
      id: 'filter-priorite',
      title: 'Filtrer par priorite',
      subtitle: 'Consulter les signalements selon leur priorite de traitement',
      icon: 'flag',
      accentColor: '#93c5fd'
    },
    {
      id: 'filter-date',
      title: 'Filtrer par date de resolution',
      subtitle: 'Choisir une plage de dates pour afficher les signalements resolus',
      icon: 'calendar-range',
      accentColor: '#86efac'
    }
  ];

  readonly statutOptions = ['EN_ATTENTE', 'OUVERTE', 'ENVOYEE', 'EN_COURS_DETUDE', 'RESOLU', 'REJETE'];
  readonly graviteOptions = ['FAIBLE', 'MOYENNE', 'ELEVEE', 'CRITIQUE'];
  readonly prioriteOptions = ['BASSE', 'MOYENNE', 'HAUTE', 'URGENTE'];
  readonly bureauProfileFields: ProfileFieldConfig[] = [
    { key: 'email', label: 'Email', icon: 'email' },
    { key: 'region', label: 'Region', icon: 'region' },
    { key: 'delegation', label: 'Delegation', icon: 'delegation' },
    { key: 'matricule', label: 'Matricule', icon: 'matricule' }
  ];

  loading = true;
  signalementFilter = 'all';
  message = '';
  userName = 'CivicLens User';
  showServicesModal = false;
  showSignalementsModal = false;
  showProfileModal = false;
  showQuickFilterModal = false;
  showAlert = false;
  alertMessage = '';
  alertType: 'success' | 'error' = 'error';
  servicesTableConfig: ServicesTableConfig = { showActions: false };
  selectedQuickFilter: QuickFilterType | null = null;
  selectedStatut = '';
  selectedGravite = '';
  selectedPriorite = '';
  selectedDateFrom = '';
  selectedDateTo = '';
  filteredSignalementsTitle = 'Tous les signalements';
  filteredSignalementsSubtitle = 'Consultez les signalements et ouvrez le detail pour chaque ligne.';
  filteredSignalementsEmptyMessage = 'Aucun signalement trouve.';
  filteredSignalementQuery: BureauSignalementFilters | null = null;
  filteredSignalementsRefreshToken = 0;
  private alertTimeoutId: ReturnType<typeof setTimeout> | null = null;
  reclamationsStats: ReclamationsStats = {
    total: 0,
    enAttente: 0,
    ouvertes: 0,
    envoyees: 0,
    enCoursDEtude: 0,
    resolus: 0,
    rejetes: 0,
    delegation: '',
    domaine: null
  };
  currentHeroSlide = 0;
  readonly heroSlides: HeroSlide[] = [
    {
      src: '/images/pexels-wal_-172619-2156618639-35977436.jpg',
      alt: 'Equipe de voirie en intervention sur la chaussee'
    },
    {
      src: '/images/pexels-sabian-mahmud-193021066-11477908.jpg',
      alt: 'Techniciens electricite en operation sur une installation'
    },
    {
      src: 'https://images.pexels.com/photos/9875441/pexels-photo-9875441.jpeg?auto=compress&cs=tinysrgb&w=1600',
      alt: 'Technicien transportant un panneau solaire sur un toit'
    },
    {
      src: 'https://images.pexels.com/photos/7656743/pexels-photo-7656743.jpeg?auto=compress&cs=tinysrgb&w=1600',
      alt: 'Equipe municipale de proprete en intervention'
    }
  ];

  constructor(
    private route: ActivatedRoute,
    private bureauDashboardService: BureauDashboardService
  ) {}

  ngOnInit(): void {
    this.userName = this.resolveUserName();

    this.route.queryParamMap.subscribe((params) => {
      this.signalementFilter = params.get('signalement') ?? 'all';
    });

    this.loadReclamationsStats();
    this.loading = false;
  }

  onServiceAction(action: 'view' | 'activate' | 'deactivate' | 'password'): void {
    this.servicesTableConfig = this.getServicesTableConfig(action);
    this.openServicesModal();
  }

  onSignalementAction(action: 'view' | 'assign'): void {
    this.filteredSignalementQuery = null;
    this.filteredSignalementsTitle = 'Tous les signalements';
    this.filteredSignalementsSubtitle = 'Consultez les signalements et ouvrez le detail pour chaque ligne.';
    this.filteredSignalementsEmptyMessage = 'Aucun signalement trouve.';
    this.filteredSignalementsRefreshToken += 1;

    if (action === 'view') {
      this.signalementFilter = 'all';
      this.openSignalementsModal();
      return;
    }

    this.signalementFilter = 'by-status';
    this.message = 'Mode affectation active: utilisez cette vue pour affecter les signalements aux organisations.';
    this.openSignalementsModal();
  }

  prevHeroSlide(): void {
    this.currentHeroSlide =
      (this.currentHeroSlide - 1 + this.heroSlides.length) % this.heroSlides.length;
  }

  nextHeroSlide(): void {
    this.currentHeroSlide = (this.currentHeroSlide + 1) % this.heroSlides.length;
  }

  onQuickAction(action: QuickActionItem): void {
    const filterTypeMap: Record<string, QuickFilterType> = {
      'filter-status': 'statut',
      'filter-gravite': 'gravite',
      'filter-priorite': 'priorite',
      'filter-date': 'date'
    };

    const filterType = filterTypeMap[action.id];
    if (!filterType) return;

    this.openQuickFilterModal(filterType);
  }

  openServicesModal(): void {
    this.showServicesModal = true;
    document.body.style.overflow = 'hidden';
  }

  closeServicesModal(): void {
    this.showServicesModal = false;
    document.body.style.overflow = '';
  }

  openSignalementsModal(): void {
    this.showSignalementsModal = true;
    document.body.style.overflow = 'hidden';
  }

  closeSignalementsModal(): void {
    this.showSignalementsModal = false;
    document.body.style.overflow = '';
  }

  onFilteredSignalementsEmpty(message: string): void {
    this.closeSignalementsModal();
    this.showSimpleAlert('error', message);
  }

  onFilteredSignalementsError(message: string): void {
    this.closeSignalementsModal();
    this.showSimpleAlert('error', message);
  }

  closeAlert(): void {
    this.showAlert = false;

    if (this.alertTimeoutId) {
      clearTimeout(this.alertTimeoutId);
      this.alertTimeoutId = null;
    }
  }

  private showSimpleAlert(type: 'success' | 'error', message: string): void {
    this.alertType = type;
    this.alertMessage = message;
    this.showAlert = true;

    if (this.alertTimeoutId) {
      clearTimeout(this.alertTimeoutId);
    }

    this.alertTimeoutId = setTimeout(() => {
      this.showAlert = false;
      this.alertTimeoutId = null;
    }, 2600);
  }

  openQuickFilterModal(filterType: QuickFilterType): void {
    this.selectedQuickFilter = filterType;
    this.showQuickFilterModal = true;
    this.message = '';
    this.resetQuickFilterInputs(filterType);
    document.body.style.overflow = 'hidden';
  }

  closeQuickFilterModal(): void {
    this.showQuickFilterModal = false;
    this.selectedQuickFilter = null;
    document.body.style.overflow = '';
  }

  applyQuickFilter(): void {
    const filter = this.buildQuickFilter();
    if (!filter) return;

    this.filteredSignalementQuery = filter.query;
    this.filteredSignalementsTitle = filter.title;
    this.filteredSignalementsSubtitle = filter.subtitle;
    this.filteredSignalementsEmptyMessage = filter.emptyMessage;
    this.filteredSignalementsRefreshToken += 1;

    this.showQuickFilterModal = false;
    this.showSignalementsModal = true;
    document.body.style.overflow = 'hidden';
  }

  openProfileModal(): void {
    this.showProfileModal = true;
    document.body.style.overflow = 'hidden';
  }

  closeProfileModal(): void {
    this.showProfileModal = false;
    document.body.style.overflow = '';
  }

  get quickFilterModalTitle(): string {
    switch (this.selectedQuickFilter) {
      case 'statut':
        return 'Filtrer les signalements par statut';
      case 'gravite':
        return 'Filtrer les signalements par gravite';
      case 'priorite':
        return 'Filtrer les signalements par priorite';
      case 'date':
        return 'Filtrer les signalements par date de resolution';
      default:
        return 'Filtrer les signalements';
    }
  }

  get quickFilterModalSubtitle(): string {
    switch (this.selectedQuickFilter) {
      case 'date':
        return 'Choisissez une date de debut et une date de fin pour afficher les signalements resolus dans cette plage.';
      default:
        return 'Selectionnez une valeur pour afficher la liste correspondante.';
    }
  }

  private getServicesTableConfig(action: 'view' | 'activate' | 'deactivate' | 'password'): ServicesTableConfig {
    if (action === 'activate') {
      return {
        showActions: true,
        actionType: 'activate'
      };
    }

    if (action === 'deactivate') {
      return {
        showActions: true,
        actionType: 'deactivate'
      };
    }

    if (action === 'password') {
      return {
        showActions: true,
        actionType: 'password'
      };
    }

    return { showActions: false };
  }

  private resetQuickFilterInputs(filterType: QuickFilterType): void {
    if (filterType === 'statut') {
      this.selectedStatut = '';
    } else if (filterType === 'gravite') {
      this.selectedGravite = '';
    } else if (filterType === 'priorite') {
      this.selectedPriorite = '';
    } else {
      this.selectedDateFrom = '';
      this.selectedDateTo = '';
    }
  }

  private buildQuickFilter(): {
    query: BureauSignalementFilters;
    title: string;
    subtitle: string;
    emptyMessage: string;
  } | null {
    if (this.selectedQuickFilter === 'statut') {
      if (!this.selectedStatut) {
        this.message = 'Veuillez choisir un statut avant de lancer le filtre.';
        return null;
      }

      return {
        query: { statut: this.selectedStatut },
        title: `Signalements - Statut ${this.selectedStatut}`,
        subtitle: `Liste des signalements filtres par statut : ${this.selectedStatut}.`,
        emptyMessage: `Aucun signalement avec le statut "${this.selectedStatut}" n'a ete trouve.`
      };
    }

    if (this.selectedQuickFilter === 'gravite') {
      if (!this.selectedGravite) {
        this.message = 'Veuillez choisir une gravite avant de lancer le filtre.';
        return null;
      }

      return {
        query: { gravite: this.selectedGravite },
        title: `Signalements - Gravite ${this.selectedGravite}`,
        subtitle: `Liste des signalements filtres par gravite : ${this.selectedGravite}.`,
        emptyMessage: `Aucun signalement avec la gravite "${this.selectedGravite}" n'a ete trouve.`
      };
    }

    if (this.selectedQuickFilter === 'priorite') {
      if (!this.selectedPriorite) {
        this.message = 'Veuillez choisir une priorite avant de lancer le filtre.';
        return null;
      }

      return {
        query: { priorite: this.selectedPriorite },
        title: `Signalements - Priorite ${this.selectedPriorite}`,
        subtitle: `Liste des signalements filtres par priorite : ${this.selectedPriorite}.`,
        emptyMessage: `Aucun signalement avec la priorite "${this.selectedPriorite}" n'a ete trouve.`
      };
    }

    if (this.selectedQuickFilter === 'date') {
      if (!this.selectedDateFrom || !this.selectedDateTo) {
        this.message = 'Veuillez definir une date de debut et une date de fin.';
        return null;
      }

      if (this.selectedDateFrom > this.selectedDateTo) {
        this.message = 'La date de debut doit etre anterieure ou egale a la date de fin.';
        return null;
      }

      return {
        query: {
          dateResolutionFrom: `${this.selectedDateFrom}T00:00:00Z`,
          dateResolutionTo: `${this.selectedDateTo}T23:59:59Z`
        },
        title: 'Signalements - Date de resolution',
        subtitle: `Liste des signalements resolus entre le ${this.formatDisplayDate(this.selectedDateFrom)} et le ${this.formatDisplayDate(this.selectedDateTo)}.`,
        emptyMessage: `Aucun signalement resolu entre le ${this.formatDisplayDate(this.selectedDateFrom)} et le ${this.formatDisplayDate(this.selectedDateTo)} n'a ete trouve.`
      };
    }

    return null;
  }

  private formatDisplayDate(value: string): string {
    if (!value) return '';

    return new Date(`${value}T00:00:00`).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private loadReclamationsStats(): void {
    this.bureauDashboardService.getStats().subscribe({
      next: (stats) => {
        this.reclamationsStats = {
          total: stats.total ?? 0,
          enAttente: stats.enAttente ?? 0,
          ouvertes: stats.ouvertes ?? 0,
          envoyees: stats.envoyees ?? 0,
          enCoursDEtude: stats.enCoursDEtude ?? 0,
          resolus: stats.resolus ?? 0,
          rejetes: stats.rejetes ?? 0,
          delegation: stats.delegation || '',
          domaine: stats.domaine ?? null
        };
      },
      error: () => {
        this.message = 'Impossible de charger les statistiques des reclamations.';
      }
    });
  }

  private resolveUserName(): string {
    const keys = ['full_name', 'username', 'userName', 'nom', 'email'];
    for (const key of keys) {
      const value = localStorage.getItem(key);
      if (value && value.trim()) {
        return value.trim();
      }
    }

    return 'CivicLens User';
  }
}
