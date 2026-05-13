import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, OnChanges, Output, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-base-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './base-modal.component.html',
  styleUrl: './base-modal.component.css'
})
export class BaseModalComponent implements OnChanges {
  @Input() visible = false;
  @Input() ariaLabel = 'Dialog';
  @Input() closeAriaLabel = 'Close dialog';
  @Input() maxWidth = '720px';
  @Input() zIndex = 950;

  @Output() close = new EventEmitter<void>();

  isRendered = false;
  isClosing = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['visible']) return;

    if (this.visible) {
      this.isRendered = true;
      this.isClosing = false;
      return;
    }

    if (this.isRendered) {
      this.isClosing = true;
      window.setTimeout(() => {
        if (!this.visible) {
          this.isRendered = false;
        }
      }, 220);
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.visible) {
      this.requestClose();
    }
  }

  requestClose(): void {
    this.close.emit();
  }
}
