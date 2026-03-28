import { Injectable } from '@angular/core';
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

@Injectable({
  providedIn: 'root'
})
export class ValidationService {
  
  static getErrorMessage(control: AbstractControl, fieldName: string): string {
    if (control.hasError('required')) {
      return 'Champ obligatoire';
    }
    
    if (control.hasError('email')) {
      return 'Email invalide';
    }
    
    if (control.hasError('pattern')) {
      if (fieldName === 'CIN') {
        return 'CIN doit contenir 8 chiffres';
      }
      return 'Format invalide';
    }
    
    if (control.hasError('minlength')) {
      const requiredLength = control.errors?.['minlength'].requiredLength;
      return `Minimum ${requiredLength} caractères`;
    }
    
    if (control.hasError('passwordMismatch')) {
      return 'Les mots de passe ne correspondent pas';
    }
    
    return '';
  }
  
  static passwordMatchValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get('password');
      const confirmPassword = control.get('confirmPassword');
      
      if (password && confirmPassword && password.value !== confirmPassword.value) {
        confirmPassword.setErrors({ passwordMismatch: true });
        return { passwordMismatch: true };
      }
      
      return null;
    };
  }
}