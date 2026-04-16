import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../services/admin.service';

@Component({
  selector: 'app-statistiques',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './statistiques.component.html',
  styleUrl: './statistiques.component.css'
})
export class StatistiquesComponent implements OnInit {
  data: any = null;
  loading = true;
  error = '';
  days = 30;
  chartOpen = false;
  activeChartKey: string | null = null;
  activeChartTitle = '';
  activeChartColor: 'blue' | 'purple' | 'green' | 'orange' | 'yellow' | 'teal' | 'dark' = 'blue';

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.adminService.getStatsTimeseries(this.days).subscribe({
      next: (d) => { this.data = d; this.loading = false; },
      error: () => { this.error = 'Erreur de chargement'; this.loading = false; }
    });
  }

  get totals(): any {
    return this.data?.totals || null;
  }

  get series(): any {
    return this.data?.series || null;
  }

  get cards(): Array<{
    key: string;
    icon: 'users' | 'alert' | 'building' | 'bank' | 'check' | 'lock';
    label: string;
    value: number;
    color: 'blue' | 'purple' | 'green' | 'orange' | 'yellow' | 'teal' | 'dark';
    seriesKey: string;
  }> {
    const t = this.totals || {};
    return [
      { key: 'citoyens', icon: 'users', label: 'Citoyens', value: t.totalCitoyens ?? 0, color: 'blue', seriesKey: 'citoyens' },
      { key: 'logins', icon: 'lock', label: 'Connexions (aujourd’hui)', value: this.series?.logins ? this.getLast(this.series.logins) : 0, color: 'dark', seriesKey: 'logins' },
      { key: 'bureaux', icon: 'building', label: 'Bureaux de Contrôle', value: t.totalBureaux ?? 0, color: 'green', seriesKey: 'bureaux' },
      { key: 'organisations', icon: 'bank', label: 'Organisations', value: t.totalOrganisations ?? 0, color: 'orange', seriesKey: 'organisations' },
      { key: 'signalements', icon: 'alert', label: 'Signalements', value: t.totalSignalements ?? 0, color: 'purple', seriesKey: 'signalements' },
      { key: 'resolus', icon: 'check', label: 'Signalements résolus', value: t.signalementsResolus ?? 0, color: 'teal', seriesKey: 'resolus' }
    ];
  }

  openChart(seriesKey: string, title: string, color: 'blue' | 'purple' | 'green' | 'orange' | 'yellow' | 'teal' | 'dark'): void {
    this.activeChartKey = seriesKey;
    this.activeChartTitle = title;
    this.activeChartColor = color;
    this.chartOpen = true;
  }

  closeChart(): void {
    this.chartOpen = false;
    this.activeChartKey = null;
    this.activeChartTitle = '';
  }

  getActiveSeries(): Array<{ date: string; value: number }> {
    if (!this.series || !this.activeChartKey) return [];
    return this.series[this.activeChartKey] || [];
  }

  getLast(series: Array<{ date: string; value: number }>): number {
    if (!series || series.length === 0) return 0;
    return series[series.length - 1]?.value ?? 0;
  }

  getLinePath(series: Array<{ date: string; value: number }>, w = 160, h = 46, pad = 6): string {
    if (!series || series.length === 0) return '';
    const values = series.map(p => Number(p.value) || 0);
    const max = Math.max(...values);
    if (max <= 0) return '';
    const min = 0;
    const innerW = Math.max(w - pad * 2, 1);
    const innerH = Math.max(h - pad * 2, 1);
    const n = values.length;
    const step = n > 1 ? innerW / (n - 1) : 0;
    const pts = values.map((v, i) => {
      const x = pad + step * i;
      const t = (v - min) / (max - min);
      const y = pad + (1 - t) * innerH;
      return `${x.toFixed(2)},${y.toFixed(2)}`;
    });
    return `M ${pts.join(' L ')}`;
  }

  getAreaPath(series: Array<{ date: string; value: number }>, w = 160, h = 46, pad = 6): string {
    const line = this.getLinePath(series, w, h, pad);
    if (!line) return '';
    const bottom = h - pad;
    const left = pad;
    const right = w - pad;
    return `${line} L ${right},${bottom} L ${left},${bottom} Z`;
  }

  getNiceMax(series: Array<{ date: string; value: number }>): number {
    if (!series || series.length === 0) return 0;
    const max = Math.max(...series.map(p => Number(p.value) || 0));
    if (max <= 0) return 0;
    const mag = Math.pow(10, Math.floor(Math.log10(max)));
    const norm = max / mag;
    const niceNorm = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
    return niceNorm * mag;
  }

  getYTicks(series: Array<{ date: string; value: number }>, w = 820, h = 260, pad = 40, ticks = 4): Array<{ y: number; value: number; label: string }> {
    const niceMax = this.getNiceMax(series);
    if (!niceMax) return [];
    const innerH = Math.max(h - pad * 2, 1);
    const step = niceMax / ticks;
    const arr: Array<{ y: number; value: number; label: string }> = [];
    for (let i = 0; i <= ticks; i++) {
      const v = step * i;
      const t = v / niceMax;
      const y = pad + (1 - t) * innerH;
      arr.push({ y, value: v, label: String(Math.round(v)) });
    }
    return arr;
  }

  formatDateShort(iso: string): string {
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return iso;
    return d.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
  }

  getXAxisLabels(series: Array<{ date: string; value: number }>): { start: string; end: string } {
    if (!series || series.length === 0) return { start: '', end: '' };
    const start = series[0]?.date || '';
    const end = series[series.length - 1]?.date || '';
    return { start: this.formatDateShort(start), end: this.formatDateShort(end) };
  }
}
