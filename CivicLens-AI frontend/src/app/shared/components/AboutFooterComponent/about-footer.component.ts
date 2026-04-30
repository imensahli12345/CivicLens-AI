import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

interface FooterStat {
  value: string;
  label: string;
}

interface FooterLink {
  label: string;
  route: string;
}

@Component({
  selector: 'app-about-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './about-footer.component.html',
  styleUrl: './about-footer.component.css'
})
export class AboutFooterComponent {
  readonly currentYear = new Date().getFullYear();

  readonly stats: FooterStat[] = [
    { value: '24', label: 'Gouvernorats' },
    { value: '24/7', label: 'Suivi temps reel' },
    { value: '100%', label: 'Roles securises' },
    { value: 'AI', label: 'Pilotage priorite' }
  ];

  readonly platformLinks: FooterLink[] = [
    { label: 'Tableau de bord', route: '/dashboard' },
    { label: 'Signalements', route: '/dashboard/signalements' },
    { label: 'Services', route: '/dashboard/services' },
    { label: 'A propos', route: '/dashboard/about' }
  ];

  readonly resourceLinks: FooterLink[] = [
    { label: 'Documentation', route: '/docs' },
    { label: 'Support', route: '/support' },
    { label: 'Confidentialite', route: '/privacy' },
    { label: 'Conditions', route: '/terms' }
  ];

  scrollToTop(event: Event): void {
    event.preventDefault();
    const top = document.getElementById('top');
    if (top) {
      top.scrollIntoView({ behavior: 'smooth', block: 'start' });
      window.history.replaceState(null, '', '#top');
    }
  }
}
