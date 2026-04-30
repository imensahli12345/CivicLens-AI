import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  BureauService,
  BureauServicesService
} from '../../../core/services/bureau-services.service';
import { SimpleAlertComponent } from '../simple-alert/simple-alert.component';

type ConfirmMode = 'activate' | 'deactivate' | null;
type ServicesActionType = 'activate' | 'deactivate';
type ServiceStatus = 'ACTIF' | 'INACTIF';

export interface ServicesTableConfig {
  showActions: boolean;
  filterStatus?: ServiceStatus;
  actionType?: ServicesActionType;
}

@Component({
  selector: 'app-bureau-services-table',
  standalone: true,
  imports: [CommonModule, SimpleAlertComponent],
  templateUrl: './bureau-services-table.component.html',
  styleUrl: './bureau-services-table.component.css'
})
export class BureauServicesTableComponent implements OnInit {
  @Input() config: ServicesTableConfig = { showActions: false };

  services: BureauService[] = [];
  loading = true;
  error = '';

  confirmMode: ConfirmMode = null;
  selectedService: BureauService | null = null;
  processing = false;

  showAlert = false;
  alertType: 'success' | 'error' = 'success';
  alertMessage = '';
  private alertTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor(private bureauServices: BureauServicesService) {}

  ngOnInit(): void {
    this.loadServices();
  }

  get filteredServices(): BureauService[] {
    if (!this.config.filterStatus) {
      return this.services;
    }

    return this.services.filter((service) => service.statut === this.config.filterStatus);
  }

  get shouldShowActionsColumn(): boolean {
    return this.config.showActions;
  }

  loadServices(): void {
    this.loading = true;
    this.error = '';
    this.bureauServices.getServices().subscribe({
      next: (services) => {
        this.services = services;
        this.loading = false;
      },
      error: () => {
        this.error = 'Impossible de charger les services.';
        this.loading = false;
      }
    });
  }

  getActionTitle(): string {
    return this.config.actionType === 'deactivate'
      ? 'Desactiver le service'
      : 'Activer le service';
  }

  onActionClick(service: BureauService): void {
    if (!this.config.showActions || !this.config.actionType) return;

    if (this.config.actionType === 'activate') {
      this.askActivate(service);
      return;
    }

    this.askDeactivate(service);
  }

  askActivate(service: BureauService): void {
    this.selectedService = service;
    this.confirmMode = 'activate';
  }

  askDeactivate(service: BureauService): void {
    this.selectedService = service;
    this.confirmMode = 'deactivate';
  }

  closeModal(): void {
    if (this.processing) return;
    this.confirmMode = null;
    this.selectedService = null;
  }

  confirm(): void {
    if (!this.selectedService || !this.confirmMode) return;
    const id = this.selectedService.id;
    const mode = this.confirmMode;
    const nextStatus: BureauService['statut'] = mode === 'activate' ? 'ACTIF' : 'INACTIF';
    const previousService = this.services.find((service) => service.id === id);
    const previousStatus = previousService?.statut ?? (nextStatus === 'ACTIF' ? 'INACTIF' : 'ACTIF');
    const action$ =
      mode === 'activate'
        ? this.bureauServices.activate(id)
        : this.bureauServices.deactivate(id);

    // Optimistic update for immediate feedback in the table.
    this.services = this.services.map((service) =>
      service.id === id ? { ...service, statut: nextStatus } : service
    );
    this.processing = true;
    action$.subscribe({
      next: () => {
        this.showSimpleAlert(
          'success',
          mode === 'activate'
            ? 'Service active avec succes'
            : 'Service desactive avec succes'
        );

        this.processing = false;
        this.confirmMode = null;
        this.selectedService = null;
      },
      error: (err: HttpErrorResponse) => {
        // Some backends return 200 with a non-JSON text body; Angular can flag that as an error.
        if (err.status === 200) {
          this.processing = false;
          this.confirmMode = null;
          this.selectedService = null;
          this.showSimpleAlert(
            'success',
            mode === 'activate'
              ? 'Service active avec succes'
              : 'Service desactive avec succes'
          );
          return;
        }

        // Rollback when API fails.
        this.services = this.services.map((service) =>
          service.id === id ? { ...service, statut: previousStatus } : service
        );

        this.processing = false;
        this.showSimpleAlert(
          'error',
          mode === 'activate'
            ? 'Erreur lors de l activation du service'
            : 'Erreur lors de la desactivation du service'
        );
        this.confirmMode = null;
        this.selectedService = null;
      }
    });
  }
  //new code getters for dynamic titles and subtitles based on config
  get tableTitle(): string {
  if (!this.config.showActions) {
    return 'Services du bureau';
  }

  if (this.config.actionType === 'activate') {
    return 'Activer les services';
  }

  if (this.config.actionType === 'deactivate') {
    return 'Desactiver les services';
  }

  return 'Services';
}

get tableSubtitle(): string {
  if (!this.config.showActions) {
    return 'Consultez tous les services rattaches a votre bureau.';
  }

  if (this.config.actionType === 'activate') {
    return 'Seuls les services inactifs sont affiches.';
  }

  if (this.config.actionType === 'deactivate') {
    return 'Seuls les services actifs sont affiches.';
  }

  return '';
}

  trackById(_: number, service: BureauService): string {
    return service.id;
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
}
