import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ValidationErrorComponent } from '../validation-error/validation-error.component';
import { OrganisationOption } from '../../../core/services/bureau-signalements.service';

interface SignalementLite {
  id: number;
  titre: string;
}

export interface AssignSignalementFormValue {
  organisationId: string;
  descriptionSupplementaire: string | null;
}

@Component({
  selector: 'app-assign-signalement',
  standalone: true,
  imports: [CommonModule, FormsModule, ValidationErrorComponent],
  templateUrl: './assign-signalement.component.html',
  styleUrls: ['./assign-signalement.component.css']
})
export class AssignSignalementComponent {
  @Input() open = false;
  @Input() signalement: SignalementLite | null = null;
  @Input() organisations: OrganisationOption[] = [];
  @Input() isAssigning = false;

  @Output() cancel = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<AssignSignalementFormValue>();

  selectedOrganisation: string | null = null;
  descriptionSupplementaire = '';
  validationMessage = '';

  onCancel(): void {
    this.reset();
    this.cancel.emit();
  }

  onConfirm(): void {
    if (!this.selectedOrganisation) {
      this.validationMessage = 'Veuillez selectionner une organisation.';
      return;
    }

    this.validationMessage = '';
    this.confirm.emit({
      organisationId: this.selectedOrganisation,
      descriptionSupplementaire: this.descriptionSupplementaire.trim() || null
    });
  }

  private reset(): void {
    this.selectedOrganisation = null;
    this.descriptionSupplementaire = '';
    this.validationMessage = '';
  }
}
