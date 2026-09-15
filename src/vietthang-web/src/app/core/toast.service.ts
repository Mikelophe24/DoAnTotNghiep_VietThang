import { Injectable, signal } from '@angular/core';

export interface Toast { id: number; message: string; type: 'success' | 'danger' | 'info'; }

/** Thông báo nổi góc phải, tự ẩn sau 3 giây. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private seq = 0;

  show(message: string, type: Toast['type'] = 'success', ms = 3000) {
    const toast: Toast = { id: ++this.seq, message, type };
    this.toasts.update(list => [...list, toast]);
    setTimeout(() => this.dismiss(toast.id), ms);
  }
  success(message: string) { this.show(message, 'success'); }
  error(message: string) { this.show(message, 'danger', 4500); }
  dismiss(id: number) { this.toasts.update(list => list.filter(t => t.id !== id)); }
}
