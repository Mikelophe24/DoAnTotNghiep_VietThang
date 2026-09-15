import { Component, inject, Input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CheckoutResult } from '../../core/models';
import { VndPipe } from '../../core/vnd.pipe';

@Component({
  selector: 'app-order-success',
  standalone: true,
  imports: [RouterLink, VndPipe],
  template: `
    <div class="container mt-4">
      <div class="row justify-content-center"><div class="col-lg-7">
        <div class="text-center mb-4">
          <i class="bi bi-check-circle-fill text-success" style="font-size:4rem"></i>
          <h1 class="h3 mt-2">Đặt hàng thành công!</h1>
          <p class="text-muted">Mã đơn hàng của bạn là <strong class="text-brand fs-5">{{ code }}</strong>.<br />Cửa hàng sẽ gọi xác nhận trong thời gian sớm nhất.</p>
        </div>
        @if (result?.bankAccount) {
          <div class="alert alert-info">
            <strong><i class="bi bi-bank"></i> Thông tin chuyển khoản</strong><br />
            {{ result!.bankAccount }}<br />
            Số tiền: <strong>{{ result!.totalAmount | vnd }}</strong> · Nội dung: <strong>{{ code }}</strong>
          </div>
        }
        <div class="d-flex gap-2 justify-content-center mt-4">
          <a class="btn btn-brand" routerLink="/">Tiếp tục mua sắm</a>
          <a class="btn btn-outline-secondary" routerLink="/tra-cuu-don-hang" [queryParams]="{ code }">Tra cứu đơn hàng</a>
        </div>
      </div></div>
    </div>
  `
})
export class OrderSuccessComponent {
  @Input() code = '';
  result: CheckoutResult | null = inject(Router).getCurrentNavigation()?.extras.state?.['result'] ?? history.state?.['result'] ?? null;
}
