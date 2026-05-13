import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import { BaseModalComponent } from '../base-modal/base-modal.component';
import { ProfileRecord, ProfileService, ProfileType } from '../../../core/services/profile.service';

export interface ProfileFieldConfig {
  key: string;
  label: string;
  icon: 'email' | 'region' | 'delegation' | 'matricule' | 'id';
}

@Component({
  selector: 'app-profile-modal',
  standalone: true,
  imports: [CommonModule, BaseModalComponent],
  templateUrl: './profile-modal.component.html',
  styleUrl: './profile-modal.component.css'
})
export class ProfileModalComponent implements OnChanges {
  @Input() visible = false;
  @Input() profileType: ProfileType = 'bureau';
  @Input() endpoint?: string;
  @Input() title = 'Profile';
  @Input() subtitle = 'Profile details';
  @Input() emptyValue = 'Not provided';
  @Input() fieldConfigs: ProfileFieldConfig[] = [
    { key: 'email', label: 'Email', icon: 'email' },
    { key: 'region', label: 'Region', icon: 'region' },
    { key: 'delegation', label: 'Delegation', icon: 'delegation' },
    { key: 'matricule', label: 'Matricule', icon: 'matricule' }
  ];

  @Output() close = new EventEmitter<void>();

  profile: ProfileRecord | null = null;
  loading = false;
  errorMessage = '';

  constructor(private profileService: ProfileService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.loadProfile();
    }
  }

  trackByKey(_: number, field: ProfileFieldConfig): string {
    return field.key;
  }

  requestClose(): void {
    this.close.emit();
  }

  getFieldValue(key: string): string {
    const value = this.profile?.[key];

    if (value === null || value === undefined || value === '') {
      return this.emptyValue;
    }

    return String(value);
  }

  getFormattedFieldValue(field: ProfileFieldConfig): string {
    const value = this.getFieldValue(field.key);

    if (field.icon !== 'email' || value === this.emptyValue) {
      return value;
    }

    return value.replace('@', '@<wbr>');
  }

  getIconPath(icon: ProfileFieldConfig['icon']): string {
    const iconMap: Record<ProfileFieldConfig['icon'], string> = {
      email: 'M4 6h16v12H4z M4 7l8 6 8-6',
      region: 'M12 21s7-4.35 7-11a7 7 0 1 0-14 0c0 6.65 7 11 7 11Z M12 13.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Z',
      delegation: 'M3 10.5 12 4l9 6.5 M5 9.5V20h14V9.5 M9 20v-5h6v5',
      matricule: 'M4 5h16v14H4z M8 9h8 M8 13h5',
      id: 'M12 3 4 7v5c0 5.2 3.4 9.9 8 11 4.6-1.1 8-5.8 8-11V7l-8-4Z'
    };

    return iconMap[icon];
  }

  private loadProfile(): void {
    this.loading = true;
    this.errorMessage = '';

    this.profileService
      .getProfile<ProfileRecord>(this.profileType, this.endpoint)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (profile) => {
          this.profile = profile;
        },
        error: (error: Error) => {
          this.profile = null;
          this.errorMessage = error.message || 'Unable to load the profile right now.';
        }
      });
  }
}
