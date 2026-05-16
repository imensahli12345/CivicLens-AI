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
  private _message = '';

  @Input()
  set message(value: string) {
    this._message = value ?? '';
    this.parseMessage();
  }

  get message(): string {
    return this._message;
  }

  displayText = '';
  linkUrl: string | null = null;

  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }

  private parseMessage(): void {
    const raw = (this._message ?? '').trim();
    if (!raw) {
      this.displayText = '';
      this.linkUrl = null;
      return;
    }

    const match = raw.match(/https?:\/\/[^\s]+|\/dashboard\/signalement\/\d+/);
    if (!match) {
      this.displayText = raw;
      this.linkUrl = null;
      return;
    }

    const matched = match[0];
    const cleanedUrl = matched.replace(/[)\].,;!?]+$/g, '');
    this.linkUrl = cleanedUrl;

    let text = raw;
    text = text.replace(matched, '');
    if (matched !== cleanedUrl) {
      text = text.replace(cleanedUrl, '');
    }
    text = text.replace(/\s{2,}/g, ' ').trim();
    text = text.replace(/[:：]\s*$/g, '').trim();

    this.displayText = text || 'Un signalement similaire existe déjà.';
  }
}
