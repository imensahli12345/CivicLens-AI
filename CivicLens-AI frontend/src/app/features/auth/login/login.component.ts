import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  disabledAccount = false;
  resetOpen = false;
  resetForm!: FormGroup;
  resetLoading = false;
  resetError = '';
  resetSuccess = '';
  activationOpen = false;
  activationForm!: FormGroup;
  activationLoading = false;
  activationError = '';
  activationSuccess = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  private isEmailOrCin(value: string): boolean {
    const v = (value ?? '').trim();
    if (!v) return false;
    if (/^\d{8}$/.test(v)) return true;
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v);
  }

  private emailOrCinValidator = (control: AbstractControl): ValidationErrors | null => {
    const v = String(control.value ?? '');
    if (!v.trim()) return null;
    return this.isEmailOrCin(v) ? null : { emailOrCin: true };
  };

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, this.emailOrCinValidator]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.resetForm = this.fb.group({
      cin: ['', [Validators.required, Validators.pattern('^[0-9]{8}$')]],
      telephone: ['', [Validators.required, Validators.pattern('^[0-9+\\s\\-]{8,20}$')]],
      nouveauMotDePasse: ['', [Validators.required, Validators.minLength(6)]],
      confirmNouveauMotDePasse: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });

    this.activationForm = this.fb.group({
      identifier: ['', [Validators.required, this.emailOrCinValidator]]
    });
  }

  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const pwd = group.get('nouveauMotDePasse')?.value;
    const confirm = group.get('confirmNouveauMotDePasse')?.value;
    if (pwd && confirm && pwd !== confirm) return { passwordMismatch: true };
    return null;
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.disabledAccount = false;

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.successMessage = 'Connexion réussie !';
        this.isLoading = false;
        setTimeout(() => {
          this.router.navigate([this.authService.getHomeRouteByRole()]);
        }, 1500);
      },
      error: (error) => {
        this.errorMessage = this.getFriendlyLoginError(error);
        this.disabledAccount = (this.errorMessage || '').toLowerCase().includes('désactiv');
        this.isLoading = false;
      }
    });
  }

  private getFriendlyLoginError(error: any): string {
    const status = error?.status;
    if (status === 0) return "Impossible de contacter le serveur. Vérifiez votre connexion ou réessayez plus tard.";
    const backendMsg = (error?.error && typeof error.error === 'string') ? error.error.trim() : '';
    if (backendMsg) return backendMsg;
    if (status === 401) return 'Email/CIN ou mot de passe incorrect.';
    return 'Impossible de se connecter pour le moment. Veuillez réessayer.';
  }

  getErrorMessage(controlName: string): string {
    const control = this.loginForm.get(controlName);
    if (control && control.invalid && control.touched) {
      if (control.hasError('required')) return 'Champ obligatoire';
      if (control.hasError('emailOrCin')) return 'Email ou CIN invalide';
      if (control.hasError('minlength')) return 'Minimum 6 caractères';
    }
    return '';
  }

  openResetModal(): void {
    this.resetOpen = true;
    this.resetError = '';
    this.resetSuccess = '';
  }

  closeResetModal(): void {
    if (this.resetLoading) return;
    this.resetOpen = false;
  }

  submitReset(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    const cin = String(this.resetForm.value.cin ?? '').trim();
    const telephone = String(this.resetForm.value.telephone ?? '').trim();
    const nouveauMotDePasse = String(this.resetForm.value.nouveauMotDePasse ?? '').trim();

    this.resetLoading = true;
    this.resetError = '';
    this.resetSuccess = '';

    this.authService.resetPasswordByCinTelephone({ cin, telephone, nouveauMotDePasse }).subscribe({
      next: (res: any) => {
        this.resetLoading = false;
        this.resetSuccess = res?.message || 'Mot de passe mis à jour. Vous pouvez vous connecter.';
        setTimeout(() => this.closeResetModal(), 1200);
      },
      error: (err: any) => {
        this.resetLoading = false;
        this.resetError = err?.message || 'Impossible de modifier le mot de passe.';
      }
    });
  }

  openActivationModal(): void {
    this.activationOpen = true;
    this.activationError = '';
    this.activationSuccess = '';
    const current = String(this.loginForm.get('email')?.value ?? '').trim();
    this.activationForm.patchValue({ identifier: current });
  }

  closeActivationModal(): void {
    if (this.activationLoading) return;
    this.activationOpen = false;
  }

  submitActivationRequest(): void {
    if (this.activationForm.invalid) {
      this.activationForm.markAllAsTouched();
      return;
    }

    const identifier = String(this.activationForm.value.identifier ?? '').trim();
    this.activationLoading = true;
    this.activationError = '';
    this.activationSuccess = '';

    this.authService.requestReactivation({ identifier }).subscribe({
      next: (res: any) => {
        this.activationLoading = false;
        this.activationSuccess = res?.message || "Demande envoyée sur la plateforme.";
      },
      error: (err: any) => {
        this.activationLoading = false;
        const apiError = err?.error;
        this.activationError =
          (typeof apiError === 'string' && apiError.trim())
            ? apiError
            : (err?.message || "Impossible d'envoyer la demande d'activation.");
      }
    });
  }
}
