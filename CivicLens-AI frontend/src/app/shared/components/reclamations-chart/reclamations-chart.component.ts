import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';

export interface ReclamationsStats {
  total: number;
  enAttente: number;
  ouvertes: number;
  envoyees: number;
  enCoursDEtude: number;
  resolus: number;
  rejetes: number;
  domaine?: string | null;
  delegation: string;
}

type ReclamationStatKey =
  | 'enAttente'
  | 'ouvertes'
  | 'envoyees'
  | 'enCoursDEtude'
  | 'resolus'
  | 'rejetes';

interface Bar {
  key: ReclamationStatKey;
  label: string;
  color: string;
  value: number;
  heightPct: number;
}

@Component({
  selector: 'app-reclamations-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reclamations-chart.component.html',
  styleUrls: ['./reclamations-chart.component.css']
})
export class ReclamationsChartComponent implements OnChanges {
  @Input() title = 'Reclamations par Statut';
  @Input() stats: ReclamationsStats = {
    total: 5,
    enAttente: 0,
    ouvertes: 0,
    envoyees: 1,
    enCoursDEtude: 0,
    resolus: 1,
    rejetes: 3,
    domaine: null,
    delegation: 'Morneg'
  };

  private readonly barConfig: ReadonlyArray<Pick<Bar, 'key' | 'label' | 'color'>> = [
    { key: 'enAttente', label: 'En Attente', color: '#a78bfa' },
    { key: 'ouvertes', label: 'Ouvertes', color: '#5eead4' },
    { key: 'envoyees', label: 'Envoyees', color: '#93c5fd' },
    { key: 'enCoursDEtude', label: 'En Cours', color: '#6b7280' },
    { key: 'resolus', label: 'Resolus', color: '#86efac' },
    { key: 'rejetes', label: 'Rejetes', color: '#fca5a5' }
  ];

  bars: Bar[] = [];
  yTicks: number[] = [];
  chartMax = 5;

  ngOnChanges(): void {
    this.compute();
  }

  ngOnInit(): void {
    this.compute();
  }

  private compute(): void {
    const rawBars: Bar[] = this.barConfig.map((cfg) => ({
      ...cfg,
      value: this.stats[cfg.key],
      heightPct: 0
    }));

    const maxVal = Math.max(...rawBars.map((bar) => bar.value), 0);
    const ticks = this.getYAxisTicks(maxVal);
    this.chartMax = ticks[ticks.length - 1];
    this.yTicks = [...ticks].reverse();

    this.bars = rawBars.map((bar) => ({
      ...bar,
      heightPct: this.chartMax === 0 ? 0 : (bar.value / this.chartMax) * 100
    }));
  }

  private getYAxisTicks(maxVal: number): number[] {
    let step = 5;

    if (maxVal > 100) step = 25;
    else if (maxVal > 50) step = 10;

    const yMax = Math.max(Math.ceil(maxVal / step) * step, step);
    const ticks: number[] = [];
    for (let value = 0; value <= yMax; value += step) ticks.push(value);
    return ticks;
  }

  get gridLines(): number[] {
    return [...this.yTicks]
      .reverse()
      .slice(1)
      .map((value) => (value / this.chartMax) * 100);
  }
}
