import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ui-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ui-modal.component.html',
  styleUrl: './ui-modal.component.scss'
})
export class UiModalComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() closeLabel = 'Đóng';
  @Input() primaryLabel = '';
  @Input() secondaryLabel = 'Hủy';
  @Input() danger = false;
  @Input() busy = false;
  @Input() disableClose = false;

  @Output() closed = new EventEmitter<void>();
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open && !this.disableClose && !this.busy) {
      this.closed.emit();
    }
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget && !this.disableClose && !this.busy) {
      this.closed.emit();
    }
  }

  close(): void {
    if (!this.disableClose && !this.busy) this.closed.emit();
  }

  cancel(): void {
    if (!this.busy) this.cancelled.emit();
  }

  confirm(): void {
    if (!this.busy) this.confirmed.emit();
  }
}
