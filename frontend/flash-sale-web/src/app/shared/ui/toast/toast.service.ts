import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastMessage {
  id: number;
  type: ToastType;
  message: string;
  dismissible: boolean;
  durationMs: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private counter = 0;
  readonly toasts = signal<ToastMessage[]>([]);

  show(message: string, type: ToastType = 'info', durationMs = 5000): number {
    const id = ++this.counter;
    const toast: ToastMessage = { id, type, message, dismissible: true, durationMs };
    this.toasts.update((list) => [...list, toast]);
    if (durationMs > 0) {
      setTimeout(() => this.dismiss(id), durationMs);
    }
    return id;
  }

  success(message: string, durationMs = 5000): number { return this.show(message, 'success', durationMs); }
  error(message: string, durationMs = 5000): number { return this.show(message, 'error', durationMs); }
  warning(message: string, durationMs = 5000): number { return this.show(message, 'warning', durationMs); }
  info(message: string, durationMs = 5000): number { return this.show(message, 'info', durationMs); }

  dismiss(id: number): void {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }

  clear(): void {
    this.toasts.set([]);
  }
}
