import { Component, inject, signal, Input, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../core/toast.service';
import { OrderDetailResponse } from '../../core/models';
import { OrderDetailsViewComponent } from '../../shared/order-details-view.component';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, FormsModule, OrderDetailsViewComponent],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h1 class="h4 mb-0">Chi tiết đơn hàng</h1>
      <a class="btn btn-outline-secondary btn-sm" routerLink="/tai-khoan/don-hang"><i class="bi bi-arrow-left"></i> Danh sách</a>
    </div>
    @if (data(); as d) {
      <div class="bg-white border rounded p-4">
        <app-order-details-view [order]="d.order" />
        @if (d.order.canCancel) {
          <div class="mt-3 d-flex gap-2">
            <input class="form-control" [(ngModel)]="reason" placeholder="Lý do hủy (không bắt buộc)" maxlength="200" />
            <button class="btn btn-outline-danger text-nowrap" (click)="cancel()"><i class="bi bi-x-circle"></i> Hủy đơn</button>
          </div>
        }
      </div>

      @if (d.order.status === 3) {
        <div class="bg-white border rounded p-4 mt-3">
          <h2 class="h6 mb-3">Đánh giá sản phẩm đã mua</h2>
          @for (p of reviewable(); track p.productId) {
            <div class="border-bottom pb-3 mb-3">
              <div class="fw-semibold">{{ p.name }}</div>
              @if (p.reviewed) {
                <div class="small text-success"><i class="bi bi-check-circle"></i> Bạn đã đánh giá sản phẩm này.</div>
              } @else {
                <div class="row g-2 align-items-start mt-1">
                  <div class="col-auto">
                    <select class="form-select form-select-sm" [(ngModel)]="p.rating">
                      <option [ngValue]="5">★★★★★ Tuyệt vời</option><option [ngValue]="4">★★★★ Hài lòng</option><option [ngValue]="3">★★★ Bình thường</option><option [ngValue]="2">★★ Chưa tốt</option><option [ngValue]="1">★ Tệ</option>
                    </select>
                  </div>
                  <div class="col"><input class="form-control form-control-sm" [(ngModel)]="p.comment" placeholder="Chia sẻ cảm nhận về chất vải, form dáng..." maxlength="1000" /></div>
                  <div class="col-auto"><button class="btn btn-sm btn-brand" (click)="review(p)">Gửi đánh giá</button></div>
                </div>
              }
            </div>
          }
        </div>
      }
    } @else { <div class="skeleton" style="height:240px"></div> }
  `
})
export class OrderDetailComponent implements OnInit {
  @Input() code = '';
  private http = inject(HttpClient);
  private toast = inject(ToastService);
  data = signal<OrderDetailResponse | null>(null);
  reviewable = signal<{ productId: number; name: string; reviewed: boolean; rating: number; comment: string }[]>([]);
  reason = '';

  ngOnInit() { this.load(); }

  load() {
    this.http.get<OrderDetailResponse>(`/api/account/orders/${this.code}`).subscribe(d => {
      this.data.set(d);
      const seen = new Set<number>();
      const items: { productId: number; name: string; reviewed: boolean; rating: number; comment: string }[] = [];
      for (const line of d.order.details) {
        const p = d.products.find(x => x.variantId === line.variantId);
        if (!p || seen.has(p.productId)) continue;
        seen.add(p.productId);
        items.push({ productId: p.productId, name: line.productName, reviewed: p.reviewed, rating: 5, comment: '' });
      }
      this.reviewable.set(items);
    });
  }

  cancel() {
    if (!confirm('Bạn chắc chắn muốn hủy đơn này?')) return;
    this.http.post<{ message: string }>(`/api/account/orders/${this.code}/cancel`, { reason: this.reason }).subscribe({
      next: r => { this.toast.success(r.message); this.load(); },
      error: e => this.toast.error(e?.error?.message ?? 'Không hủy được đơn.')
    });
  }

  review(p: { productId: number; rating: number; comment: string }) {
    this.http.post<{ message: string }>(`/api/account/orders/${this.code}/reviews`, { productId: p.productId, rating: p.rating, comment: p.comment }).subscribe({
      next: r => { this.toast.success(r.message); this.load(); },
      error: e => this.toast.error(e?.error?.message ?? 'Không gửi được đánh giá.')
    });
  }
}
