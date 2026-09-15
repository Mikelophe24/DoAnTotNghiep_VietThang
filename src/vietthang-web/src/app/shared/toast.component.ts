import { Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  template: `
    <div class="toast-area">
      @for (t of toasts.toasts(); track t.id) {
        <div class="toast show align-items-center text-white border-0" [class]="'toast show align-items-center text-white border-0 bg-' + t.type" role="alert">
          <div class="d-flex">
            <div class="toast-body">{{ t.message }}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" (click)="toasts.dismiss(t.id)"></button>
          </div>
        </div>
      }
    </div>
  `
})
export class ToastComponent {
  toasts = inject(ToastService);
}
