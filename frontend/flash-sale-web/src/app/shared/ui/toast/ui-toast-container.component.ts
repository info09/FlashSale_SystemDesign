import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastMessage } from './toast.service';

@Component({
  selector: 'ui-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ui-toast-container.component.html',
  styleUrl: './ui-toast-container.component.scss'
})
export class UiToastContainerComponent {
  private readonly toastService = inject(ToastService);
  readonly toasts = this.toastService.toasts;

  dismiss(id: number): void {
    this.toastService.dismiss(id);
  }

  icon(type: ToastMessage['type']): string {
    switch (type) {
      case 'success': return '✓';
      case 'error': return '!';
      case 'warning': return '!';
      default: return 'i';
    }
  }
}
