import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { EvenementService } from '../../../../core/services/evenement.service';
import { CreateEvenementRequest } from '../../../../shared/models/evenement.model';

@Component({
  selector: 'app-nouveau-evenement',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './nouveau-evenement.component.html',
  styleUrls: ['./nouveau-evenement.component.css']
})
export class NouveauEvenementComponent {
  formData: CreateEvenementRequest = {
    titre: '',
    description: '',
    dateEvenement: '',
    lieu: '',
    nombreVolontairesRequis: 1
  };
  
  isSubmitting = false;
  errorMsg = '';

  constructor(private evenementService: EvenementService, private router: Router) {}

  onSubmit(): void {
    if (!this.formData.titre || !this.formData.description || !this.formData.dateEvenement || !this.formData.lieu) {
      this.errorMsg = 'Veuillez remplir tous les champs obligatoires.';
      return;
    }

    this.isSubmitting = true;
    this.errorMsg = '';

    // Transformer la date string html locale ("YYYY-MM-DDTHH:mm") en Date UTC ou correcte
    const dateObj = new Date(this.formData.dateEvenement);

    const request: CreateEvenementRequest = {
      ...this.formData,
      dateEvenement: dateObj.toISOString()
    };

    this.evenementService.create(request).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        this.router.navigate(['/dashboard/evenements']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMsg = "Une erreur s'est produite lors de la création de l'événement.";
        console.error(err);
      }
    });
  }
}
