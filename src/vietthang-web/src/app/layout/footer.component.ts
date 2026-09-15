import { Component, inject } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CatalogService } from '../core/catalog.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, AsyncPipe, DecimalPipe],
  template: `
    <footer class="site-footer mt-5">
      <div class="container py-5">
        <div class="row g-4">
          <div class="col-md-4">
            <h5 class="footer-title">{{ (settings$ | async)?.storeName || 'Thời trang Việt Thắng' }}</h5>
            <p class="small mb-2">Đồ mặc nhà, đồ bộ chất liệu lanh và cotton cho cả gia đình: Nữ, Trung niên, Trẻ em, Nam.</p>
            @if (settings$ | async; as s) {
              <p class="small mb-1"><i class="bi bi-telephone"></i> Hotline: <a href="tel:{{ s.hotline }}">{{ s.hotline }}</a></p>
              <p class="small mb-0"><i class="bi bi-envelope"></i> <a href="mailto:{{ s.email }}">{{ s.email }}</a></p>
            }
          </div>
          <div class="col-md-3">
            <h5 class="footer-title">Hỗ trợ khách hàng</h5>
            <ul class="list-unstyled small footer-links">
              <li><a routerLink="/tra-cuu-don-hang">Tra cứu đơn hàng</a></li>
              <li><a routerLink="/lien-he">Liên hệ</a></li>
              <li><a routerLink="/tuyen-dung">Tuyển dụng</a></li>
              @if (settings$ | async; as s) { <li>Miễn phí vận chuyển cho đơn từ {{ s.freeShippingThreshold | number:'1.0-0' }} đ</li> }
            </ul>
          </div>
          <div class="col-md-5">
            <h5 class="footer-title">Hệ thống cửa hàng</h5>
            <ul class="list-unstyled small footer-links">
              @for (s of stores$ | async; track s.id) {
                <li><i class="bi bi-geo-alt"></i> <strong>{{ s.name }}</strong>: {{ s.address }}</li>
              }
              <li><a routerLink="/he-thong-cua-hang">Xem tất cả cửa hàng →</a></li>
            </ul>
          </div>
        </div>
      </div>
      <div class="footer-bottom py-3">
        <div class="container small d-flex flex-wrap justify-content-between">
          <span>© 2026 Thời trang gia đình VT</span>
          <span>Đồ án tốt nghiệp · Angular + ASP.NET Core Web API</span>
        </div>
      </div>
    </footer>
  `
})
export class FooterComponent {
  private catalog = inject(CatalogService);
  settings$ = this.catalog.settings();
  stores$ = this.catalog.stores();
}
