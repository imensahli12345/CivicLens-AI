import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-simple-alert',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './simple-alert.component.html',
  styleUrl: './simple-alert.component.css'
})
export class SimpleAlertComponent {
  @Input() open = false;
  @Input() type: 'success' | 'error' = 'success';
  @Input() message = '';

  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }
}
