import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (totalPages > 1) {
      <nav class="mt-4">
        <ul class="pagination justify-content-center mb-0">
          <li class="page-item" [class.disabled]="page <= 1"><button class="page-link" (click)="go(page - 1)">‹</button></li>
          @for (i of pages(); track i) {
            <li class="page-item" [class.active]="i === page"><button class="page-link" (click)="go(i)">{{ i }}</button></li>
          }
          <li class="page-item" [class.disabled]="page >= totalPages"><button class="page-link" (click)="go(page + 1)">›</button></li>
        </ul>
      </nav>
    }
  `
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 1;
  @Output() pageChange = new EventEmitter<number>();

  pages(): number[] {
    const from = Math.max(1, this.page - 2), to = Math.min(this.totalPages, this.page + 2);
    return Array.from({ length: to - from + 1 }, (_, i) => from + i);
  }
  go(p: number) { if (p >= 1 && p <= this.totalPages && p !== this.page) this.pageChange.emit(p); }
}
