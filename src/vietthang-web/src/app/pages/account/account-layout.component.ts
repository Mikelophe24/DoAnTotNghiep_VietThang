import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-account-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="container mt-3"><div class="row g-4">
      <div class="col-lg-3">
        <div class="list-group customer-nav">
          <a class="list-group-item list-group-item-action" routerLink="/tai-khoan" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"><i class="bi bi-person"></i> Hồ sơ của tôi</a>
          <a class="list-group-item list-group-item-action" routerLink="/tai-khoan/don-hang" routerLinkActive="active"><i class="bi bi-receipt"></i> Đơn hàng của tôi</a>
          <a class="list-group-item list-group-item-action" routerLink="/tai-khoan/dia-chi" routerLinkActive="active"><i class="bi bi-geo-alt"></i> Sổ địa chỉ</a>
          <a class="list-group-item list-group-item-action" routerLink="/tai-khoan/yeu-thich" routerLinkActive="active"><i class="bi bi-heart"></i> Sản phẩm yêu thích</a>
          <button class="list-group-item list-group-item-action text-danger" (click)="logout()"><i class="bi bi-box-arrow-right"></i> Đăng xuất</button>
        </div>
      </div>
      <div class="col-lg-9"><router-outlet /></div>
    </div></div>
  `
})
export class AccountLayoutComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  logout() { this.auth.logout(); this.router.navigateByUrl('/'); }
}
