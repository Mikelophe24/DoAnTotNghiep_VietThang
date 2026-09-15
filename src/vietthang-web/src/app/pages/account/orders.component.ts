import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { OrderSummary, Paged, statusBadge } from '../../core/models';
import { VndPipe } from '../../core/vnd.pipe';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [RouterLink, DatePipe, VndPipe, PaginationComponent],
  template: `
    <h1 class="h4 mb-3">Đơn hàng của tôi</h1>
    @if (data(); as d) {
      @if (!d.items.length) {
        <div class="bg-white border rounded p-5 text-center text-muted">Bạn chưa có đơn hàng nào. <a routerLink="/">Mua sắm ngay</a></div>
      } @else {
        <div class="bg-white border rounded">
          <table class="table mb-0 align-middle">
            <thead class="table-light"><tr><th>Mã đơn</th><th>Ngày đặt</th><th class="text-center">SP</th><th class="text-end">Tổng tiền</th><th class="text-center">Trạng thái</th><th></th></tr></thead>
            <tbody>
              @for (o of d.items; track o.id) {
                <tr>
                  <td><a [routerLink]="['/tai-khoan/don-hang', o.orderCode]" class="fw-semibold">{{ o.orderCode }}</a></td>
                  <td class="small">{{ o.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td class="text-center">{{ o.itemCount }}</td>
                  <td class="text-end fw-semibold">{{ o.totalAmount | vnd }}</td>
                  <td class="text-center"><span [class]="'badge ' + badge(o.status)">{{ o.statusName }}</span></td>
                  <td class="text-end"><a class="btn btn-sm btn-outline-secondary" [routerLink]="['/tai-khoan/don-hang', o.orderCode]">Chi tiết</a></td>
                </tr>
              }
            </tbody>
          </table>
        </div>
        <app-pagination [page]="d.pageIndex" [totalPages]="d.totalPages" (pageChange)="load($event)" />
      }
    } @else { <div class="skeleton" style="height:160px"></div> }
  `
})
export class OrdersComponent implements OnInit {
  private http = inject(HttpClient);
  data = signal<Paged<OrderSummary> | null>(null);
  badge = statusBadge;
  ngOnInit() { this.load(1); }
  load(page: number) { this.http.get<Paged<OrderSummary>>('/api/account/orders', { params: { page } }).subscribe(d => this.data.set(d)); }
}
