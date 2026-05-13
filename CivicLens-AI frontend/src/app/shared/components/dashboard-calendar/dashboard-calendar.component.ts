import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

interface CalendarCell {
  day: number | null;
}

@Component({
  selector: 'app-dashboard-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-calendar.component.html',
  styleUrls: ['./dashboard-calendar.component.css']
})
export class DashboardCalendarComponent {
  readonly weekDays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  currentMonthDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
  selectedDate = new Date();
  cells: CalendarCell[] = [];

  constructor() {
    this.refreshCells();
  }

  get monthLabel(): string {
    return this.currentMonthDate.toLocaleDateString('fr-FR', {
      month: 'long',
      year: 'numeric'
    }).replace(/^./, (char) => char.toUpperCase());
  }

  previousMonth(): void {
    this.currentMonthDate = new Date(
      this.currentMonthDate.getFullYear(),
      this.currentMonthDate.getMonth() - 1,
      1
    );
    this.refreshCells();
  }

  nextMonth(): void {
    this.currentMonthDate = new Date(
      this.currentMonthDate.getFullYear(),
      this.currentMonthDate.getMonth() + 1,
      1
    );
    this.refreshCells();
  }

  selectDay(day: number): void {
    this.selectedDate = new Date(
      this.currentMonthDate.getFullYear(),
      this.currentMonthDate.getMonth(),
      day
    );
  }

  isSelected(day: number | null): boolean {
    if (day === null) {
      return false;
    }

    return this.selectedDate.getFullYear() === this.currentMonthDate.getFullYear()
      && this.selectedDate.getMonth() === this.currentMonthDate.getMonth()
      && this.selectedDate.getDate() === day;
  }

  private refreshCells(): void {
    const year = this.currentMonthDate.getFullYear();
    const month = this.currentMonthDate.getMonth();
    const firstWeekDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const cells: CalendarCell[] = [];
    for (let index = 0; index < firstWeekDay; index += 1) {
      cells.push({ day: null });
    }
    for (let day = 1; day <= daysInMonth; day += 1) {
      cells.push({ day });
    }
    while (cells.length % 7 !== 0) {
      cells.push({ day: null });
    }

    this.cells = cells;
  }
}
