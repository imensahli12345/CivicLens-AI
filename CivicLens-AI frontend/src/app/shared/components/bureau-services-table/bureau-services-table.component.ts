import { CommonModule } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import {
  BureauService,
  BureauServicesService
} from '../../../core/services/bureau-services.service';
import { SimpleAlertComponent } from '../simple-alert/simple-alert.component';

type ConfirmMode = 'activate' | 'deactivate' | 'password' | null;
type ServicesActionType = 'activate' | 'deactivate' | 'password';
type ServiceStatus = 'ACTIF' | 'INACTIF';

export interface ServicesTableConfig {
  showActions: boolean;
  filterStatus?: ServiceStatus;
  actionType?: ServicesActionType;
}

@Component({
  selector: 'app-bureau-services-table',
  standalone: true,
  imports: [CommonModule, FormsModule, SimpleAlertComponent],
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
  passwordValue = '';
  passwordConfirm = '';
  bureauPassword = '';
  passwordError = '';

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
    if (!this.config.showActions) return false;
    return this.config.actionType !== 'password';
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
    if (this.config.actionType === 'deactivate') return 'Desactiver le service';
    if (this.config.actionType === 'password') return 'Modifier le mot de passe';
    return 'Activer le service';
  }

  canPerformAction(service: BureauService): boolean {
    if (!this.config.showActions || !this.config.actionType) return false;
    if (this.config.actionType === 'password') return true;
    if (this.config.actionType === 'activate') return service.statut === 'INACTIF';
    return service.statut === 'ACTIF';
  }

  getRowActionTitle(service: BureauService): string {
    if (!this.config.showActions || !this.config.actionType) return '';
    if (this.config.actionType === 'password') return 'Modifier mot de passe';
    if (this.config.actionType === 'activate') {
      return service.statut === 'ACTIF' ? 'Deja actif' : 'Activer le service';
    }
    return service.statut === 'INACTIF' ? 'Deja inactif' : 'Desactiver le service';
  }

  onActionClick(service: BureauService): void {
    if (!this.config.showActions || !this.config.actionType) return;

    if (this.config.actionType === 'password') {
      this.askPassword(service);
      return;
    }

    if (!this.canPerformAction(service)) {
      this.showSimpleAlert(
        'error',
        this.config.actionType === 'activate'
          ? 'Ce service est deja actif.'
          : 'Ce service est deja inactif.'
      );
      return;
    }

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

  askPassword(service: BureauService): void {
    this.selectedService = service;
    this.confirmMode = 'password';
    this.passwordValue = '';
    this.passwordConfirm = '';
    this.bureauPassword = '';
    this.passwordError = '';
  }

  closeModal(): void {
    if (this.processing) return;
    this.confirmMode = null;
    this.selectedService = null;
    this.passwordValue = '';
    this.passwordConfirm = '';
    this.bureauPassword = '';
    this.passwordError = '';
  }

  confirm(): void {
    if (!this.selectedService || !this.confirmMode) return;
    if (this.confirmMode === 'password') return;
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

  confirmPassword(): void {
    if (!this.selectedService || this.confirmMode !== 'password') return;
    const id = this.selectedService.id;
    const next = String(this.passwordValue ?? '').trim();
    const confirm = String(this.passwordConfirm ?? '').trim();
    const bureauPwd = String(this.bureauPassword ?? '').trim();

    this.passwordError = '';
    if (!next || next.length < 6) {
      this.passwordError = 'Mot de passe invalide (min 6 caractères).';
      return;
    }
    if (next !== confirm) {
      this.passwordError = 'Les mots de passe ne correspondent pas.';
      return;
    }
    if (!bureauPwd) {
      this.passwordError = 'Mot de passe du bureau requis.';
      return;
    }

    this.processing = true;
    this.bureauServices.updatePassword(id, next, bureauPwd).subscribe({
      next: () => {
        this.processing = false;
        this.confirmMode = null;
        this.selectedService = null;
        this.passwordValue = '';
        this.passwordConfirm = '';
        this.bureauPassword = '';
        this.showSimpleAlert('success', 'Mot de passe du service mis à jour.');
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 200) {
          this.processing = false;
          this.confirmMode = null;
          this.selectedService = null;
          this.passwordValue = '';
          this.passwordConfirm = '';
          this.bureauPassword = '';
          this.showSimpleAlert('success', 'Mot de passe du service mis à jour.');
          return;
        }

        const apiError = err?.error;
        this.processing = false;
        this.passwordError =
          (typeof apiError === 'string' && apiError.trim())
            ? apiError
            : 'Erreur lors de la mise à jour du mot de passe.';
      }
    });
  }

  onRowClick(service: BureauService): void {
    if (this.config.actionType !== 'password') return;
    this.askPassword(service);
  }

  displayServiceName(name: string): string {
    const cleaned = String(name ?? '').trim();
    if (!cleaned) return '';
    return cleaned.replace(/^service\s+/i, '').trim();
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

  if (this.config.actionType === 'password') {
    return 'Modifier les mots de passe';
  }

  return 'Services';
}

get tableSubtitle(): string {
  if (!this.config.showActions) {
    return 'Consultez tous les services rattaches a votre bureau.';
  }

  if (this.config.actionType === 'activate') {
    return 'Cliquez sur un service inactif pour l activer.';
  }

  if (this.config.actionType === 'deactivate') {
    return 'Cliquez sur un service actif pour le desactiver.';
  }

  if (this.config.actionType === 'password') {
    return 'Cliquez sur un service pour definir un nouveau mot de passe.';
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
