import { Component, inject, signal, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ProductCard } from '../../core/models';
import { ProductCardComponent } from '../../shared/product-card.component';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-wishlist',
  standalone: true,
  imports: [ProductCardComponent],
  template: `
    <h1 class="h4 mb-3">Sản phẩm yêu thích <span class="text-muted fs-6">({{ items().length }})</span></h1>
    @if (!items().length) {
      <div class="bg-white border rounded p-5 text-center text-muted">Chưa có sản phẩm yêu thích. Bấm <i class="bi bi-heart"></i> ở trang sản phẩm để lưu.</div>
    } @else {
      <div class="row g-3 row-cols-2 row-cols-md-3">
        @for (p of items(); track p.id) {
          <div class="col">
            <app-product-card [p]="p" />
            <button class="btn btn-sm btn-link text-danger w-100" (click)="remove(p)"><i class="bi bi-heartbreak"></i> Bỏ yêu thích</button>
          </div>
        }
      </div>
    }
  `
})
export class WishlistComponent implements OnInit {
  private http = inject(HttpClient);
  private toast = inject(ToastService);
  items = signal<ProductCard[]>([]);
  ngOnInit() { this.http.get<ProductCard[]>('/api/account/wishlist').subscribe(l => this.items.set(l)); }
  remove(p: ProductCard) {
    this.http.post<{ message: string }>(`/api/account/wishlist/${p.id}/toggle`, {}).subscribe(r => { this.toast.success(r.message); this.items.update(l => l.filter(x => x.id !== p.id)); });
  }
}
