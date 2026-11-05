import { Component, inject } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CatalogService } from '../core/catalog.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, AsyncPipe, DecimalPipe],
  template: `
    <footer class="site-footer">
      <div class="container py-5">
        <div class="row g-4 g-lg-5">
          <div class="col-lg-4">
            <a class="footer-brand" routerLink="/">
              <span class="brand-mark">VT</span>
              <strong>{{ (settings$ | async)?.storeName || 'Thời trang Việt Thắng' }}</strong>
            </a>
            <p class="small mb-3">Đồ mặc nhà, đồ bộ chất liệu lanh và cotton tự nhiên cho cả gia đình: Nữ, Trung niên, Trẻ em, Nam. May tại xưởng Việt Thắng.</p>
            @if (settings$ | async; as s) {
              <ul class="list-unstyled small footer-contact mb-0">
                <li><i class="bi bi-telephone"></i><span>Hotline: <a href="tel:{{ s.hotline }}">{{ s.hotline }}</a> (8:30 – 21:30)</span></li>
                <li><i class="bi bi-envelope"></i><a href="mailto:{{ s.email }}">{{ s.email }}</a></li>
              </ul>
            }
          </div>
          <div class="col-6 col-lg-2">
            <h5 class="footer-title">Mua sắm</h5>
            <ul class="list-unstyled small footer-links">
              <li><a routerLink="/new-arrivals">Hàng mới về</a></li>
              <li><a routerLink="/sale">Khuyến mãi</a></li>
              <li><a routerLink="/category/nu">Đồ nữ</a></li>
              <li><a routerLink="/category/nam">Đồ nam</a></li>
              <li><a routerLink="/category/tre-em">Trẻ em</a></li>
            </ul>
          </div>
          <div class="col-6 col-lg-2">
            <h5 class="footer-title">Hỗ trợ</h5>
            <ul class="list-unstyled small footer-links">
              <li><a routerLink="/order-lookup">Tra cứu đơn hàng</a></li>
              <li><a routerLink="/contact">Liên hệ</a></li>
              <li><a routerLink="/news">Tin tức</a></li>
              <li><a routerLink="/careers">Tuyển dụng</a></li>
              @if (settings$ | async; as s) { <li>Freeship đơn từ {{ s.freeShippingThreshold | number:'1.0-0' }} đ</li> }
            </ul>
          </div>
          <div class="col-lg-4">
            <h5 class="footer-title">Hệ thống cửa hàng</h5>
            <ul class="list-unstyled small footer-contact">
              @for (s of stores$ | async; track s.id) {
                <li><i class="bi bi-geo-alt"></i><span><strong>{{ s.name }}</strong><br />{{ s.address }}</span></li>
              }
            </ul>
            <a class="small fw-semibold" routerLink="/stores">Xem tất cả cửa hàng <i class="bi bi-arrow-right"></i></a>
          </div>
        </div>
      </div>
      <div class="footer-bottom pb-4">
        <div class="container d-flex flex-wrap justify-content-between gap-2">
          <span>© 2026 Thời trang Việt Thắng</span>
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
