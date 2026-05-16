import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TUNISIA_REGIONS } from '../../../shared/data/tunisia-regions';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  resetOpen = false;
  resetForm!: FormGroup;
  resetLoading = false;
  resetError = '';
  resetSuccess = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  regions = Object.keys(TUNISIA_REGIONS);
  delegations: string[] = [];

  onRegionChange(event: Event): void {
    const region = (event.target as HTMLSelectElement).value;
    this.delegations = TUNISIA_REGIONS[region as keyof typeof TUNISIA_REGIONS] || [];
    this.registerForm.get('delegation')?.setValue('');
  }

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      nom: ['', [Validators.required, Validators.minLength(2)]],
      prenom: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      region: ['', [Validators.required, Validators.minLength(3)]],
      delegation: ['', [Validators.required, Validators.minLength(3)]],
      cin: ['', [Validators.required, Validators.pattern('^[0-9]{8}$')]]
    }, {
      validators: this.passwordMatchValidator
    });

    this.resetForm = this.fb.group({
      cin: ['', [Validators.required, Validators.pattern('^[0-9]{8}$')]],
      telephone: ['', [Validators.required, Validators.pattern('^[0-9+\\s\\-]{8,20}$')]],
      nouveauMotDePasse: ['', [Validators.required, Validators.minLength(6)]],
      confirmNouveauMotDePasse: ['', [Validators.required]]
    }, {
      validators: this.resetPasswordMatchValidator
    });
  }

  // Validateur personnalisé pour vérifier que les mots de passe correspondent
  passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    
    if (password && confirmPassword && password !== confirmPassword) {
      return { passwordMismatch: true };
    }
    return null;
  }

  resetPasswordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const pwd = group.get('nouveauMotDePasse')?.value;
    const confirm = group.get('confirmNouveauMotDePasse')?.value;
    if (pwd && confirm && pwd !== confirm) return { passwordMismatch: true };
    return null;
  }

  onSubmit(): void {
    // Vérifier si le formulaire est invalide
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Extraire les données du formulaire sans le champ confirmPassword
    const { confirmPassword, ...registerData } = this.registerForm.value;

    // Appeler l'API d'inscription
    this.authService.register(registerData).subscribe({
      next: (response) => {
        // Succès de l'inscription
        this.successMessage = 'Inscription réussie ! Redirection vers la page de connexion...';
        this.isLoading = false;
        
        // Rediriger vers la page de connexion après 1.5 secondes
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: (error) => {
        // Gestion des erreurs
        this.errorMessage = this.getFriendlyRegisterError(error);
        this.isLoading = false;
      }
    });
  }

  private getFriendlyRegisterError(error: any): string {
    const status = error?.status;
    if (status === 0) return "Impossible de contacter le serveur. Vérifiez votre connexion ou réessayez plus tard.";

    const backendMsg = (error?.error && typeof error.error === 'string') ? error.error.trim() : '';
    if (backendMsg) return backendMsg;

    if (status === 409) return "Un compte avec cet email existe déjà.";
    if (status === 400) return "Informations invalides. Vérifiez les champs puis réessayez.";

    return "Impossible de créer le compte pour le moment. Veuillez réessayer.";
  }

  onCinInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const rawValue = input.value ?? '';
    const digitsOnly = rawValue.replace(/\D+/g, '').slice(0, 8);

    if (digitsOnly !== rawValue) {
      input.value = digitsOnly;
    }

    const cinControl = this.registerForm.get('cin');
    if (cinControl && cinControl.value !== digitsOnly) {
      cinControl.setValue(digitsOnly, { emitEvent: false });
    }
  }

  // Méthode pour obtenir les messages d'erreur spécifiques à chaque champ
  getErrorMessage(controlName: string): string {
    const control = this.registerForm.get(controlName);
    
    if (control && control.invalid && (control.touched || control.dirty)) {
      if (control.hasError('required')) {
        return 'Champ obligatoire';
      }
      
      if (control.hasError('email')) {
        return 'Email invalide';
      }
      
      if (control.hasError('pattern')) {
        if (controlName === 'cin') {
          return 'CIN doit contenir exactement 8 chiffres';
        }
        return 'Format invalide';
      }
      
      if (control.hasError('minlength')) {
        const requiredLength = control.errors?.['minlength'].requiredLength;
        return `Minimum ${requiredLength} caractères`;
      }
    }
    
    // Vérifier l'erreur de correspondance des mots de passe au niveau du formulaire
    if (controlName === 'confirmPassword' && this.registerForm.hasError('passwordMismatch') && control?.touched) {
      return 'Les mots de passe ne correspondent pas';
    }
    
    return '';
  }

  // Getter pour faciliter l'accès aux champs dans le template
  get nom() { return this.registerForm.get('nom'); }
  get prenom() { return this.registerForm.get('prenom'); }
  get email() { return this.registerForm.get('email'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }
  get region() { return this.registerForm.get('region'); }
  get delegation() { return this.registerForm.get('delegation'); }
  get cin() { return this.registerForm.get('cin'); }

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
}
