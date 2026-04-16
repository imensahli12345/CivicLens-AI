import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.css']
})
export class AboutComponent {
  features = [
    {
      icon: '🚨',
      title: 'Signalement citoyen',
      desc: 'Déclarez tout problème urbain — voirie, éclairage, propreté, eau, espaces verts — en quelques secondes depuis votre appareil.'
    },
    {
      icon: '📍',
      title: 'Géolocalisation précise',
      desc: 'Chaque signalement est ancré dans une région et une adresse. Les services concernés reçoivent automatiquement les alertes.'
    },
    {
      icon: '👍',
      title: 'Vote citoyen',
      desc: 'Soutenez les signalements de vos voisins par un vote. Les problèmes les plus soutenus remontent en priorité.'
    },
    {
      icon: '🔄',
      title: 'Suivi en temps réel',
      desc: 'Suivez l\'état de chaque signalement : en attente, en cours de traitement, résolu ou rejeté.'
    },
    {
      icon: '🔒',
      title: 'Accès sécurisé',
      desc: 'Authentification par JWT, rôles distincts (Citoyen, Service, Bureau de contrôle) et données chiffrées.'
    },
    {
      icon: '🤖',
      title: 'Intelligence artificielle',
      desc: 'CivicLens AI analyse les signalements, détecte les tendances et aide les gestionnaires à prioriser les interventions.'
    },
  ];

  stats = [
    { value: '24 gouvernorats', label: 'Tunisie entière couverte' },
    { value: 'Accès sécurisé',  label: 'Sécurité multicouche' },
    { value: '7 catégories',    label: 'De problèmes urbains' },
    { value: '100% open',       label: 'Données citoyennes' },
  ];
}