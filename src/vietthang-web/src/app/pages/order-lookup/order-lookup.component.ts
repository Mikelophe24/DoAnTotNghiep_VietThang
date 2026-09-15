import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { OrderDetail } from '../../core/models';
import { OrderDetailsViewComponent } from '../../shared/order-details-view.component';

@Component({
  selector: 'app-order-lookup',
  standalone: true,
  imports: [FormsModule, OrderDetailsViewComponent],
  template: `
    <div class="container mt-3">
      <div class="row justify-content-center"><div class="col-lg-8">
        <h1 class="h4 mb-3">Tra cứu đơn hàng</h1>
        <form class="bg-white border rounded p-4 mb-4" (ngSubmit)="lookup()">
          <div class="row g-3 align-items-end">
            <div class="col-md-5"><label class="form-label">Mã đơn hàng</label><input class="form-control text-uppercase" name="code" [(ngModel)]="code" placeholder="VT260915001" required /></div>
            <div class="col-md-5"><label class="form-label">Số điện thoại</label><input class="form-control" name="phone" [(ngModel)]="phone" placeholder="Số điện thoại khi đặt hàng" required /></div>
            <div class="col-md-2"><button class="btn btn-brand w-100" [disabled]="loading()"><i class="bi bi-search"></i> Tra cứu</button></div>
          </div>
        </form>
        @if (error()) { <div class="alert alert-warning">{{ error() }}</div> }
        @if (order()) { <div class="bg-white border rounded p-4"><app-order-details-view [order]="order()!" /></div> }
      </div></div>
    </div>
  `
})
export class OrderLookupComponent implements OnInit {
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  code = '';
  phone = '';
  order = signal<OrderDetail | null>(null);
  error = signal<string | null>(null);
  loading = signal(false);

  ngOnInit() { this.code = this.route.snapshot.queryParamMap.get('code') ?? ''; }

  lookup() {
    if (!this.code.trim() || !this.phone.trim()) { this.error.set('Nhập mã đơn hàng và số điện thoại.'); return; }
    this.loading.set(true); this.error.set(null); this.order.set(null);
    this.http.get<OrderDetail>(`/api/checkout/orders/${this.code.trim().toUpperCase()}`, { params: { phone: this.phone.trim() } }).subscribe({
      next: o => { this.order.set(o); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.message ?? 'Không tìm thấy đơn hàng.'); this.loading.set(false); }
    });
  }
}
