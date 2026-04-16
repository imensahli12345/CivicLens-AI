import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { ApiService } from '../../../../core/services/api.service';
import { User } from '../../../../shared/models/user.model';
import { TUNISIA_REGIONS } from '../../../../shared/data/tunisia-regions';

@Component({
  selector: 'app-profil-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './profil-edit.component.html',
  styleUrls: ['./profil-edit.component.css']
})
export class ProfilEditComponent implements OnInit {
  user = signal<User | null>(null);
  form!: FormGroup;
  loading = signal(false);
  passwordLoading = signal(false);
  photoSaving = signal(false);
  photoDirty = signal(false);
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);
  photoPreview: string | null = null;
  newPassword = '';
  confirmNewPassword = '';

  // Régions et délégations
  regions = Object.keys(TUNISIA_REGIONS);
  delegations: string[] = [];

  constructor(
    private authService: AuthService,
    private apiService: ApiService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.authService.fetchMe().subscribe({
      next: (u) => {
        this.user.set(u);
        this.photoPreview = u.photoUrl || null;
        
        // Initialiser les délégations si une région est déjà sélectionnée
        if (u?.region) {
          this.delegations = TUNISIA_REGIONS[u.region as keyof typeof TUNISIA_REGIONS] || [];
        }
        
        this.form = this.fb.group({
          nom:     [u?.nom     || '', [Validators.required]],
          prenom:  [u?.prenom  || '', [Validators.required]],
          email:   [{ value: u?.email || '', disabled: true }],
          telephone: [u?.telephone || '', [Validators.pattern('^[+]?[0-9\\s]{8,20}$')]],
          region:     [u?.region     || '', []],
          delegation: [u?.delegation || '', []],
          photoUrl:   [u?.photoUrl   || ''],
        });
        this.loading.set(false);
      },
      error: () => {
        const u = this.authService.getCurrentUser();
        this.user.set(u);
        this.photoPreview = u?.photoUrl || null;
        this.form = this.fb.group({
          nom:     [u?.nom     || '', [Validators.required]],
          prenom:  [u?.prenom  || '', [Validators.required]],
          email:   [{ value: u?.email || '', disabled: true }],
          telephone: [u?.telephone || '', [Validators.pattern('^[+]?[0-9\\s]{8,20}$')]],
          region:     [u?.region     || '', []],
          delegation: [u?.delegation || '', []],
          photoUrl:   [u?.photoUrl   || ''],
        });
        this.loading.set(false);
      }
    });
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    if (!file.type.startsWith('image/')) {
      this.errorMsg.set('Veuillez sélectionner une image valide.');
      return;
    }
    const reader = new FileReader();
    reader.onload = () => {
      this.photoPreview = reader.result as string;
      this.form.patchValue({ photoUrl: this.photoPreview });
      this.photoDirty.set(true);
      this.successMsg.set(null);
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.successMsg.set(null);
    this.errorMsg.set(null);

    const raw = this.form.getRawValue();
    const payload = {
      ...raw,
      photoUrl: this.photoDirty() ? this.photoPreview : (raw.photoUrl || this.user()?.photoUrl || null),
    };

    this.apiService.put('/auth/me', payload).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMsg.set('Profil mis à jour avec succès.');
        const u = this.user()!;
        const updatedUser = {
          ...u,
          nom: raw.nom,
          prenom: raw.prenom,
          telephone: raw.telephone, // Ajouter le téléphone
          region: raw.region,
          delegation: raw.delegation,
          photoUrl: this.photoDirty() ? this.photoPreview : (payload.photoUrl ?? u.photoUrl ?? null),
        };
        this.user.set(updatedUser);
        
        // Sauvegarder dans le localStorage pour que le dashboard puisse détecter les changements
        localStorage.setItem('current_user', JSON.stringify(updatedUser));
        
        // Forcer la vérification du dashboard après un court délai
        setTimeout(() => {
          window.dispatchEvent(new CustomEvent('profile-updated', { detail: updatedUser }));
        }, 100);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err.message || 'Erreur lors de la mise à jour');
      }
    });
  }

  changePassword(): void {
    this.successMsg.set(null);
    this.errorMsg.set(null);

    if (!this.newPassword && !this.confirmNewPassword) {
      this.errorMsg.set('Saisissez le nouveau mot de passe et la confirmation.');
      return;
    }
    if (this.newPassword !== this.confirmNewPassword) {
      this.errorMsg.set('Les deux mots de passe ne correspondent pas.');
      return;
    }

    this.passwordLoading.set(true);
    this.apiService.put('/auth/me/password', { nouveauMotDePasse: this.newPassword }).subscribe({
      next: () => {
        this.passwordLoading.set(false);
        this.successMsg.set('Mot de passe modifié avec succès.');
        this.newPassword = '';
        this.confirmNewPassword = '';
      },
      error: (err) => {
        this.passwordLoading.set(false);
        this.errorMsg.set(err.message || 'Impossible de modifier le mot de passe.');
      }
    });
  }

  savePhoto(): void {
    if (!this.photoDirty()) return;

    this.photoSaving.set(true);
    this.successMsg.set(null);
    this.errorMsg.set(null);

    const payload = { ...this.form.getRawValue(), photoUrl: this.photoPreview || '' };

    this.apiService.put('/auth/me', payload).subscribe({
      next: () => {
        this.photoSaving.set(false);
        this.photoDirty.set(false);
        this.successMsg.set('Photo de profil enregistrée.');
        const updated = { ...this.user()!, ...payload, photoUrl: this.photoPreview };
        this.user.set(updated);
      },
      error: (err) => {
        this.photoSaving.set(false);
        this.errorMsg.set(err.message || 'Impossible d’enregistrer la photo.');
      }
    });
  }

  hasError(field: string, error: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.touched && ctrl?.hasError(error));
  }

  onRegionChange(event: Event): void {
    const region = (event.target as HTMLSelectElement).value;
    this.delegations = TUNISIA_REGIONS[region as keyof typeof TUNISIA_REGIONS] || [];
    this.form.get('delegation')?.setValue('');
  }
}
