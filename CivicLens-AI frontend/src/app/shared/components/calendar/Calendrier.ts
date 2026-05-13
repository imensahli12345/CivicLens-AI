import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface CalendarDay {
  date: number | null;
  fullDate: Date | null;
  isToday: boolean;
  isSelected: boolean;
}

@Component({
  selector: 'app-dashboard-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-calendar.component.html',
  styleUrls: ['./dashboard-calendar.component.css']
})
export class DashboardCalendarComponent implements OnInit {
  @Output() datesConfirmed = new EventEmitter<Date[]>();

  readonly MONTH_NAMES = [
    'Janvier','Février','Mars','Avril','Mai','Juin',
    'Juillet','Août','Septembre','Octobre','Novembre','Décembre'
  ];
  readonly DAY_LABELS = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];

  viewYear  = new Date().getFullYear();
  viewMonth = new Date().getMonth();
  selectedDates: Date[] = [];
  calendarDays: CalendarDay[] = [];

  ngOnInit(): void { this.buildCalendar(); }

  get monthLabel(): string {
    return `${this.MONTH_NAMES[this.viewMonth]} ${this.viewYear}`;
  }

  prevMonth(): void {
    this.viewMonth === 0
      ? (this.viewMonth = 11, this.viewYear--)
      : this.viewMonth--;
    this.buildCalendar();
  }

  nextMonth(): void {
    this.viewMonth === 11
      ? (this.viewMonth = 0, this.viewYear++)
      : this.viewMonth++;
    this.buildCalendar();
  }

  toggleDay(day: CalendarDay): void {
    if (!day.fullDate) return;
    const key = day.fullDate.toDateString();
    const idx = this.selectedDates.findIndex(d => d.toDateString() === key);
    idx >= 0 ? this.selectedDates.splice(idx, 1) : this.selectedDates.push(day.fullDate);
    this.buildCalendar();
  }

  isSelected(day: CalendarDay): boolean {
    return !!day.fullDate &&
      this.selectedDates.some(d => d.toDateString() === day.fullDate!.toDateString());
  }

  cancel(): void { this.selectedDates = []; this.buildCalendar(); }

  confirm(): void { this.datesConfirmed.emit([...this.selectedDates]); }

  private buildCalendar(): void {
    const today        = new Date();
    const firstWeekday = new Date(this.viewYear, this.viewMonth, 1).getDay();
    const daysInMonth  = new Date(this.viewYear, this.viewMonth + 1, 0).getDate();
    const days: CalendarDay[] = [];

    for (let i = 0; i < firstWeekday; i++)
      days.push({ date: null, fullDate: null, isToday: false, isSelected: false });

    for (let d = 1; d <= daysInMonth; d++) {
      const fd = new Date(this.viewYear, this.viewMonth, d);
      days.push({
        date: d, fullDate: fd,
        isToday:    fd.toDateString() === today.toDateString(),
        isSelected: this.selectedDates.some(s => s.toDateString() === fd.toDateString())
      });
    }
    this.calendarDays = days;
  }
}