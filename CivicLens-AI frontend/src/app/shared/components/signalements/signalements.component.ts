import { CommonModule } from '@angular/common';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Component, EventEmitter, HostListener, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import {
  AssignSignalementComponent,
  AssignSignalementFormValue
} from '../assign-signalement/assign-signalement.component';
import { SignalementHistoriqueComponent } from './signalement-historique.component';
import {
  BureauSignalement,
  BureauSignalementFilters,
  BureauSignalementsService,
  HistoriqueEntry,
  OrganisationOption
} from '../../../core/services/bureau-signalements.service';
import {
  OrganisationRapportUploadResponse,
  OrganisationSignalementsService
} from '../../../core/services/organisation-signalements.service';
import { ServiceSignalementsService } from '../../../core/services/service-signalements.service';
import { SimpleAlertComponent } from '../simple-alert/simple-alert.component';

type SignalementsSource = 'bureau' | 'service' | 'organisation';

@Component({
  selector: 'app-signalements',
  standalone: true,
  imports: [CommonModule, FormsModule, AssignSignalementComponent, SignalementHistoriqueComponent, SimpleAlertComponent],
  templateUrl: './signalements.component.html',
  styleUrls: ['./signalements.component.css']
})
export class SignalementsComponent implements OnInit, OnChanges {
  @Input() title = 'Signalements';
  @Input() subtitle = 'Consultez les signalements et ouvrez le detail pour chaque ligne.';
  @Input() filters: BureauSignalementFilters | null = null;
  @Input() emptyStateMessage = 'Aucun signalement trouve.';
  @Input() refreshToken = 0;
  @Input() source: SignalementsSource = 'bureau';
  @Output() filteredEmpty = new EventEmitter<string>();
  @Output() filteredRequestError = new EventEmitter<string>();

  signalements: BureauSignalement[] = [];
  isLoading = true;
  error: string | null = null;
  private initialized = false;

  selectedSignalement: BureauSignalement | null = null;
  showDetailModal = false;
  parsedAdresse: any = null;

  lightboxOpen = false;
  lightboxIndex = 0;

  selectedForAssign: BureauSignalement | null = null;
  showAssignModal = false;
  selectedOrganisation: string | null = null;
  isAssigning = false;

  openingSignalementIds = new Set<number>();
  rejectingSignalementIds = new Set<number>();
  resolvingSignalementIds = new Set<number>();
  uploadingRapportSignalementIds = new Set<number>();

  showHistoriqueModal = false;
  historiqueLoading = false;
  historiqueError: string | null = null;
  historiqueEntries: HistoriqueEntry[] = [];
  historiqueTitle = 'Historique du signalement';

  showToast = false;
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  private toastTimeoutId: ReturnType<typeof setTimeout> | null = null;

  organisations: OrganisationOption[] = [];

  constructor(
    private bureauSignalements: BureauSignalementsService,
    private serviceSignalements: ServiceSignalementsService,
    private organisationSignalements: OrganisationSignalementsService
  ) {}

  ngOnInit(): void {
    this.initialized = true;
    if (this.canAssignSignalement()) {
      this.loadOrganisations();
    }
    this.fetchSignalements();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.initialized) return;

    if (changes['filters'] || changes['refreshToken']) {
      this.fetchSignalements();
    }
  }

  private loadOrganisations(): void {
    this.bureauSignalements.getOrganisations().subscribe({
      next: (items) => {
        this.organisations = items;
      }
    });
  }

  fetchSignalements(): void {
    this.isLoading = true;
    this.error = null;

    this.activeSignalementsService.getSignalements(this.filters).subscribe({
      next: (data) => {
        this.signalements = data ?? [];
        this.isLoading = false;

        if (this.signalements.length === 0 && this.hasActiveFilters()) {
          this.filteredEmpty.emit(this.getEmptyFilterToastMessage());
        }
      },
      error: (err: HttpErrorResponse) => {
        const message = this.extractBackendMessage(err, 'Impossible de charger les signalements. Veuillez reessayer.');

        if (this.hasActiveFilters()) {
          this.error = null;
          this.filteredRequestError.emit(message);
        } else {
          this.error = message;
        }

        this.isLoading = false;
      }
    });
  }

  openDetail(s: BureauSignalement): void {
    this.selectedSignalement = s;
    this.showDetailModal = true;
    document.body.style.overflow = 'hidden';

    this.activeSignalementsService.getSignalementById(s.id).subscribe({
      next: (detail) => {
        this.selectedSignalement = detail;
        this.parseSelectedAdresse();
      },
      error: () => {
        this.parseSelectedAdresse();
      }
    });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.selectedSignalement = null;
    document.body.style.overflow = '';
  }

  openLightbox(index: number): void {
    this.lightboxIndex = index;
    this.lightboxOpen = true;
  }

  closeLightbox(): void {
    this.lightboxOpen = false;
  }

  prevImage(event?: Event): void {
    event?.stopPropagation();
    if (!this.selectedSignalement) return;
    const len = this.selectedSignalement.images.length;
    this.lightboxIndex = (this.lightboxIndex - 1 + len) % len;
  }

  nextImage(event?: Event): void {
    event?.stopPropagation();
    if (!this.selectedSignalement) return;
    const len = this.selectedSignalement.images.length;
    this.lightboxIndex = (this.lightboxIndex + 1) % len;
  }

  @HostListener('document:keydown', ['$event'])
  handleKeydown(event: KeyboardEvent): void {
    if (!this.lightboxOpen) return;
    if (event.key === 'Escape') this.closeLightbox();
    if (event.key === 'ArrowLeft') this.prevImage();
    if (event.key === 'ArrowRight') this.nextImage();
  }

  onOuvrir(signalement: BureauSignalement): void {
    if (this.openingSignalementIds.has(signalement.id)) return;

    this.openingSignalementIds.add(signalement.id);
    const nextStatut = this.isOrganisationSource() ? 'EN_COURS_DETUDE' : 'OUVERTE';
    this.ouvrirSignalementRequest(signalement.id).subscribe({
      next: (res: HttpResponse<unknown>) => {
        this.openingSignalementIds.delete(signalement.id);
        this.updateSignalementLocal(signalement.id, { statut: nextStatut });
        this.triggerToast(
          this.getSuccessMessage(res?.body, `Le signalement "${signalement.titre}" a ete ouvert avec succes.`),
          'success'
        );
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 200) {
          this.openingSignalementIds.delete(signalement.id);
          this.updateSignalementLocal(signalement.id, { statut: nextStatut });
          this.triggerToast(`Le signalement "${signalement.titre}" a ete ouvert avec succes.`, 'success');
          return;
        }

        this.openingSignalementIds.delete(signalement.id);
        this.triggerToast(this.extractBackendMessage(err, 'Erreur lors de l ouverture du signalement.'), 'error');
      }
    });
  }

  onRejeter(signalement: BureauSignalement): void {
    if (this.rejectingSignalementIds.has(signalement.id)) return;

    this.rejectingSignalementIds.add(signalement.id);
    this.rejeterSignalementRequest(signalement.id).subscribe({
      next: (res: HttpResponse<unknown>) => {
        this.rejectingSignalementIds.delete(signalement.id);
        this.updateSignalementLocal(signalement.id, { statut: 'REJETEE' });
        this.triggerToast(
          this.getSuccessMessage(res?.body, `Le signalement "${signalement.titre}" a ete rejete avec succes.`),
          'success'
        );
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 200) {
          this.rejectingSignalementIds.delete(signalement.id);
          this.updateSignalementLocal(signalement.id, { statut: 'REJETEE' });
          this.triggerToast(`Le signalement "${signalement.titre}" a ete rejete avec succes.`, 'success');
          return;
        }

        this.rejectingSignalementIds.delete(signalement.id);
        this.triggerToast(this.extractBackendMessage(err, 'Erreur lors du rejet du signalement.'), 'error');
      }
    });
  }

  isOpening(signalementId: number): boolean {
    return this.openingSignalementIds.has(signalementId);
  }

  isRejecting(signalementId: number): boolean {
    return this.rejectingSignalementIds.has(signalementId);
  }

  isResolving(signalementId: number): boolean {
    return this.resolvingSignalementIds.has(signalementId);
  }

  isUploadingRapport(signalementId: number): boolean {
    return this.uploadingRapportSignalementIds.has(signalementId);
  }

  openAssign(s: BureauSignalement): void {
    if (!this.canAssignSignalement()) return;

    this.selectedForAssign = s;
    this.selectedOrganisation = null;
    this.showAssignModal = true;
    document.body.style.overflow = 'hidden';
  }

  openAssignFromDetail(): void {
    const current = this.selectedSignalement;
    if (!current) return;

    this.showDetailModal = false;
    this.selectedSignalement = null;
    this.openAssign(current);
  }

  closeAssign(): void {
    this.showAssignModal = false;
    this.selectedForAssign = null;
    document.body.style.overflow = '';
  }

  onAssignConfirm(form: AssignSignalementFormValue): void {
    if (!this.selectedForAssign) return;

    const signalementId = this.selectedForAssign.id;
    const signalementTitle = this.selectedForAssign.titre;
    const selectedOrg = this.organisations.find((org) => org.id === form.organisationId);

    this.selectedOrganisation = form.organisationId;
    this.isAssigning = true;

    this.assignmentServiceForCurrentSource(signalementId, form, selectedOrg?.nomOrganisation ?? null)
      .subscribe({
        next: (res) => {
          this.isAssigning = false;
          this.updateSignalementLocal(signalementId, {
            organisationNom: selectedOrg?.nomOrganisation ?? this.selectedForAssign?.organisationNom ?? '',
            statut: 'EN_COURS'
          });
          this.closeAssign();
          this.triggerToast(
            this.getSuccessMessage(res?.body, `Le signalement "${signalementTitle}" a ete assigne avec succes.`),
            'success'
          );
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 200) {
            this.isAssigning = false;
            this.updateSignalementLocal(signalementId, {
              organisationNom: selectedOrg?.nomOrganisation ?? this.selectedForAssign?.organisationNom ?? '',
              statut: 'EN_COURS'
            });
            this.closeAssign();
            this.triggerToast(`Le signalement "${signalementTitle}" a ete assigne avec succes.`, 'success');
            return;
          }

          this.isAssigning = false;
          this.triggerToast(this.extractBackendMessage(err, 'Erreur lors de l assignation du signalement.'), 'error');
        }
      });
  }

  onResoudre(signalement: BureauSignalement): void {
    if (!this.isOrganisationSource() || this.resolvingSignalementIds.has(signalement.id)) return;

    this.resolvingSignalementIds.add(signalement.id);
    this.organisationSignalements.resoudreSignalement(signalement.id).subscribe({
      next: (res) => {
        this.resolvingSignalementIds.delete(signalement.id);
        this.updateSignalementLocal(signalement.id, { statut: 'RESOLU' });
        this.triggerToast(
          this.getSuccessMessage(res?.body, `Le signalement "${signalement.titre}" a ete marque comme resolu.`),
          'success'
        );
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 200) {
          this.resolvingSignalementIds.delete(signalement.id);
          this.updateSignalementLocal(signalement.id, { statut: 'RESOLU' });
          this.triggerToast(`Le signalement "${signalement.titre}" a ete marque comme resolu.`, 'success');
          return;
        }

        this.resolvingSignalementIds.delete(signalement.id);
        this.triggerToast(this.extractBackendMessage(err, 'Erreur lors de la resolution du signalement.'), 'error');
      }
    });
  }

  openRapportPicker(signalement: BureauSignalement, fileInput: HTMLInputElement): void {
    if (!this.isOrganisationSource()) return;

    this.selectedSignalement = this.selectedSignalement?.id === signalement.id ? this.selectedSignalement : signalement;
    fileInput.value = '';
    fileInput.click();
  }

  onRapportSelected(event: Event): void {
    if (!this.isOrganisationSource() || !this.selectedSignalement) return;

    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const signalement = this.selectedSignalement;
    this.uploadingRapportSignalementIds.add(signalement.id);

    this.organisationSignalements.uploadRapport(signalement.id, file).subscribe({
      next: (res: OrganisationRapportUploadResponse) => {
        this.uploadingRapportSignalementIds.delete(signalement.id);
        this.triggerToast(
          this.getSuccessMessage(res, `Le rapport du signalement "${signalement.titre}" a ete televerse avec succes.`),
          'success'
        );
      },
      error: (err: HttpErrorResponse) => {
        this.uploadingRapportSignalementIds.delete(signalement.id);
        this.triggerToast(this.extractBackendMessage(err, 'Erreur lors du televersement du rapport.'), 'error');
      }
    });
  }

  openHistorique(signalement: BureauSignalement): void {
    if (!this.supportsHistorique()) return;

    this.historiqueTitle = `Historique - ${signalement.titre}`;
    this.historiqueEntries = [];
    this.historiqueError = null;
    this.historiqueLoading = true;
    this.showHistoriqueModal = true;

    this.historiqueSignalementsService.getHistoriqueSignalement(signalement.id).subscribe({
      next: (entries) => {
        this.historiqueEntries = entries ?? [];
        this.historiqueLoading = false;
      },
      error: (err: HttpErrorResponse) => {
        this.historiqueError = this.extractBackendMessage(err, 'Impossible de charger l historique.');
        this.historiqueLoading = false;
      }
    });
  }

  closeHistorique(): void {
    this.showHistoriqueModal = false;
    this.historiqueEntries = [];
    this.historiqueError = null;
    this.historiqueLoading = false;
  }

  triggerToast(message: string, type: 'success' | 'error' = 'success'): void {
    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;

    if (this.toastTimeoutId) {
      clearTimeout(this.toastTimeoutId);
    }
    this.toastTimeoutId = setTimeout(() => {
      this.showToast = false;
      this.toastTimeoutId = null;
    }, 2600);
  }

  getStatutClass(statut: string): string {
    const map: Record<string, string> = {
      ENVOYEE: 'badge-envoyee',
      EN_COURS: 'badge-encours',
      EN_COURS_DETUDE: 'badge-encours',
      OUVERTE: 'badge-encours',
      RESOLU: 'badge-resolue',
      RESOLUE: 'badge-resolue',
      REJETEE: 'badge-rejetee',
      REJETE: 'badge-rejetee'
    };
    return map[statut] || 'badge-default';
  }

  getGraviteClass(gravite: string): string {
    const map: Record<string, string> = {
      FAIBLE: 'gravite-faible',
      MOYENNE: 'gravite-moyenne',
      ELEVEE: 'gravite-elevee',
      CRITIQUE: 'gravite-critique'
    };
    return map[gravite] || '';
  }

  getPrioriteClass(priorite: string): string {
    const map: Record<string, string> = {
      BASSE: 'priorite-basse',
      MOYENNE: 'priorite-moyenne',
      HAUTE: 'priorite-haute',
      URGENTE: 'priorite-urgente'
    };
    return map[priorite] || '';
  }

  getDomaineIcon(domaine: string): string {
    const map: Record<string, string> = {
      ECLAIRAGE: '[EL]',
      VOIRIE: '[VO]',
      ASSAINISSEMENT: '[AS]',
      'ESPACES VERTS': '[EV]',
      TRANSPORT: '[TR]'
    };
    return map[domaine] || '[--]';
  }

  getDomaineLabel(domaine: string | null | undefined): string {
    const value = (domaine ?? '').trim();
    return value || 'Non renseigne';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  getImageUrl(path: string): string {
    return `http://localhost:5242${path}`;
  }

  shouldShowContainer(): boolean {
    if (this.isLoading || this.error) return true;
    return !(this.hasActiveFilters() && this.signalements.length === 0);
  }

  shouldShowTable(): boolean {
    if (this.isLoading || this.error) return false;
    if (this.signalements.length > 0) return true;
    return !this.hasActiveFilters();
  }

  shouldShowAssignActions(): boolean {
    return this.canAssignSignalement();
  }

  shouldShowOpenActions(): boolean {
    return this.source === 'bureau' || this.source === 'service' || this.source === 'organisation';
  }

  shouldShowRejectActions(): boolean {
    return this.source === 'bureau' || this.source === 'service';
  }

  shouldShowResolveActions(): boolean {
    return this.isOrganisationSource();
  }

  shouldShowRapportActions(): boolean {
    return this.isOrganisationSource();
  }

  shouldShowHistoryActions(): boolean {
    return !this.isOrganisationSource();
  }

  shouldShowInlineViewAction(): boolean {
    return !this.isOrganisationSource();
  }

  private updateSignalementLocal(id: number, patch: Partial<BureauSignalement>): void {
    this.signalements = this.signalements.map((signalement) =>
      signalement.id === id ? { ...signalement, ...patch } : signalement
    );

    if (this.selectedSignalement?.id === id) {
      this.selectedSignalement = { ...this.selectedSignalement, ...patch };
    }
  }

  private get activeSignalementsService():
    | BureauSignalementsService
    | ServiceSignalementsService
    | OrganisationSignalementsService {
    if (this.isBureauSource()) return this.bureauSignalements;
    if (this.isServiceSource()) return this.serviceSignalements;
    return this.organisationSignalements;
  }

  private get historiqueSignalementsService(): BureauSignalementsService | ServiceSignalementsService {
    return this.isBureauSource() ? this.bureauSignalements : this.serviceSignalements;
  }

  private ouvrirSignalementRequest(signalementId: number): Observable<HttpResponse<unknown>> {
    if (this.isBureauSource()) {
      return this.bureauSignalements.ouvrirSignalement(signalementId) as Observable<HttpResponse<unknown>>;
    }

    if (this.isServiceSource()) {
      return this.serviceSignalements.ouvrirSignalement(signalementId) as Observable<HttpResponse<unknown>>;
    }

    return this.organisationSignalements.ouvrirSignalement(signalementId) as Observable<HttpResponse<unknown>>;
  }

  private rejeterSignalementRequest(signalementId: number): Observable<HttpResponse<unknown>> {
    return this.isBureauSource()
      ? this.bureauSignalements.rejeterSignalement(signalementId) as Observable<HttpResponse<unknown>>
      : this.serviceSignalements.rejeterSignalement(signalementId) as Observable<HttpResponse<unknown>>;
  }

  private assignmentServiceForCurrentSource(
    signalementId: number,
    form: AssignSignalementFormValue,
    organisationNom: string | null
  ) {
    const payload = {
      organisationId: form.organisationId,
      organisationNom,
      descriptionSupplementaire: form.descriptionSupplementaire
    };

    return this.isBureauSource()
      ? this.bureauSignalements.assignerSignalement(signalementId, payload)
      : this.serviceSignalements.envoyerSignalement(signalementId, payload);
  }

  private canAssignSignalement(): boolean {
    return this.source === 'bureau' || this.source === 'service';
  }

  private isBureauSource(): boolean {
    return this.source === 'bureau';
  }

  private isServiceSource(): boolean {
    return this.source === 'service';
  }

  private isOrganisationSource(): boolean {
    return this.source === 'organisation';
  }

  private supportsHistorique(): boolean {
    return !this.isOrganisationSource();
  }

  private parseSelectedAdresse(): void {
    try {
      this.parsedAdresse = this.selectedSignalement?.adresse ? JSON.parse(this.selectedSignalement.adresse) : null;
    } catch {
      this.parsedAdresse = null;
    }
  }

  private hasActiveFilters(): boolean {
    if (!this.filters) return false;
    return Object.values(this.filters).some((value) => !!String(value ?? '').trim());
  }

  private getEmptyFilterToastMessage(): string {
    const statut = this.filters?.statut?.trim();
    const gravite = this.filters?.gravite?.trim();
    const priorite = this.filters?.priorite?.trim();
    const dateFrom = this.filters?.dateResolutionFrom?.trim();
    const dateTo = this.filters?.dateResolutionTo?.trim();

    if (statut) {
      return `Aucun signalement avec le statut "${statut}" n'a ete trouve.`;
    }

    if (gravite) {
      return `Aucun signalement avec la gravite "${gravite}" n'a ete trouve.`;
    }

    if (priorite) {
      return `Aucun signalement avec la priorite "${priorite}" n'a ete trouve.`;
    }

    if (dateFrom && dateTo) {
      return `Aucun signalement resolu entre le ${this.formatDate(dateFrom)} et le ${this.formatDate(dateTo)} n'a ete trouve.`;
    }

    return 'Aucun signalement ne correspond au filtre selectionne.';
  }

  private getSuccessMessage(body: unknown, fallback: string): string {
    if (typeof body === 'string' && body.trim()) return body.trim();
    if (body && typeof body === 'object') {
      const message = (body as Record<string, unknown>)['message'];
      if (typeof message === 'string' && message.trim()) return message.trim();
    }
    return fallback;
  }

  private extractBackendMessage(error: HttpErrorResponse, fallback: string): string {
    const payload = error?.error;

    if (typeof payload === 'string' && payload.trim()) {
      return payload.trim();
    }

    if (payload && typeof payload === 'object') {
      const asRecord = payload as Record<string, unknown>;
      const candidates = ['message', 'Message', 'error', 'Error', 'title', 'Title', 'detail', 'Detail'];
      for (const key of candidates) {
        const value = asRecord[key];
        if (typeof value === 'string' && value.trim()) return value.trim();
      }
    }

    if (error?.message && error.message.trim()) return error.message.trim();
    return fallback;
  }
}
