import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { OrderDetail, statusBadge } from '../core/models';
import { VndPipe } from '../core/vnd.pipe';

/** Khối hiển thị chi tiết đơn, dùng chung cho tra cứu và trang tài khoản. */
@Component({
  selector: 'app-order-details-view',
  standalone: true,
  imports: [DatePipe, VndPipe],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3">
      <div>
        <div class="small text-muted">Mã đơn</div>
        <div class="fw-bold fs-5">{{ order.orderCode }}</div>
        <div class="small text-muted">Đặt lúc {{ order.createdAt | date:'HH:mm dd/MM/yyyy' }}</div>
      </div>
      <div class="text-end">
        <span class="badge fs-6" [class]="'badge fs-6 ' + badge(order.status)">{{ order.statusName }}</span>
        <div class="small text-muted mt-1">{{ order.paymentMethodName }} · {{ order.paymentStatusName }}</div>
      </div>
    </div>
    <div class="row g-3 mb-3">
      <div class="col-md-6"><div class="small text-muted">Người nhận</div><div><strong>{{ order.customerName }}</strong> · {{ order.phone }}</div>@if (order.email) { <div class="small">{{ order.email }}</div> }</div>
      <div class="col-md-6"><div class="small text-muted">Địa chỉ giao hàng</div><div>{{ order.shippingAddress }}, {{ order.ward }}, {{ order.district }}, {{ order.province }}</div>@if (order.note) { <div class="small text-muted">Ghi chú: {{ order.note }}</div> }</div>
    </div>
    <table class="table table-sm align-middle">
      <thead class="table-light"><tr><th>Sản phẩm</th><th class="text-center">SL</th><th class="text-end">Đơn giá</th><th class="text-end">Thành tiền</th></tr></thead>
      <tbody>
        @for (d of order.details; track d.id) {
          <tr><td>{{ d.productName }}<div class="small text-muted">{{ d.colorName }} / {{ d.sizeName }} · {{ d.sku }}</div></td><td class="text-center">{{ d.quantity }}</td><td class="text-end">{{ d.unitPrice | vnd }}</td><td class="text-end">{{ d.lineTotal | vnd }}</td></tr>
        }
      </tbody>
      <tfoot>
        <tr><td colspan="3" class="text-end">Tiền hàng</td><td class="text-end">{{ order.subTotal | vnd }}</td></tr>
        <tr><td colspan="3" class="text-end">Phí vận chuyển</td><td class="text-end">{{ order.shippingFee === 0 ? 'Miễn phí' : (order.shippingFee | vnd) }}</td></tr>
        @if (order.discountAmount > 0) { <tr><td colspan="3" class="text-end">Giảm giá</td><td class="text-end text-success">−{{ order.discountAmount | vnd }}</td></tr> }
        <tr class="fw-bold"><td colspan="3" class="text-end">Tổng thanh toán</td><td class="text-end">{{ order.totalAmount | vnd }}</td></tr>
      </tfoot>
    </table>
    @if (order.cancelReason) { <div class="text-danger small">Lý do hủy: {{ order.cancelReason }}</div> }
    @if (order.histories.length) {
      <div class="small text-muted">Lịch sử</div>
      <ul class="timeline small">
        @for (h of order.histories; track $index) { <li><span class="text-muted">{{ h.changedAt | date:'HH:mm dd/MM' }}</span> · <strong>{{ h.statusName }}</strong> {{ h.note ? '– ' + h.note : '' }}</li> }
      </ul>
    }
  `
})
export class OrderDetailsViewComponent {
  @Input({ required: true }) order!: OrderDetail;
  badge = statusBadge;
}
