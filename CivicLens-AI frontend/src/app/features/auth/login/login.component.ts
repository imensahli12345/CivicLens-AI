import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: (response) => {
        this.successMessage = 'Connexion réussie !';
        this.isLoading = false;
        
        // Option 1: Rediriger vers une page d'accueil temporaire
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 1500);
        
        // Option 2: Afficher un message de succès et rester sur la page
        // (à vous de choisir selon vos besoins)
      },
      error: (error) => {
        this.errorMessage = this.getFriendlyLoginError(error);
        this.isLoading = false;
      }
    });
  }

  private getFriendlyLoginError(error: any): string {
    const status = error?.status;
    if (status === 0) return "Impossible de contacter le serveur. Vérifiez votre connexion ou réessayez plus tard.";
    if (status === 401) return 'Email ou mot de passe incorrect.';

    const backendMsg = (error?.error && typeof error.error === 'string') ? error.error.trim() : '';
    if (backendMsg) return backendMsg;

    return 'Impossible de se connecter pour le moment. Veuillez réessayer.';
  }

  getErrorMessage(controlName: string): string {
    const control = this.loginForm.get(controlName);
    if (control && control.invalid && control.touched) {
      if (control.hasError('required')) return 'Champ obligatoire';
      if (control.hasError('email')) return 'Email invalide';
      if (control.hasError('minlength')) return 'Minimum 6 caractères';
    }
    return '';
  }
}
