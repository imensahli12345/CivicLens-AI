import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'CivicLens-Ai';
  ngOnInit(): void {
    this.applyTheme(localStorage.getItem('civiclens-theme'));
  }

  private applyTheme(raw: string | null): void {
    const normalized = (raw ?? 'auto').toLowerCase();
    const mode =
      normalized === 'dark' || normalized === 'light' || normalized === 'auto'
        ? normalized
        : 'auto';

    const shouldDark =
      mode === 'dark'
        ? true
        : mode === 'light'
          ? false
          : window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;

    document.documentElement.classList.toggle('theme-dark', shouldDark);
  }
}
