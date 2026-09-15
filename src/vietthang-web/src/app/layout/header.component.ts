import { Component, inject } from '@angular/core';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { CartService } from '../core/cart.service';
import { CatalogService } from '../core/catalog.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, FormsModule, AsyncPipe, DecimalPipe],
  templateUrl: './header.component.html'
})
export class HeaderComponent {
  auth = inject(AuthService);
  cart = inject(CartService);
  catalog = inject(CatalogService);
  private router = inject(Router);

  menu$ = this.catalog.menu();
  settings$ = this.catalog.settings();
  q = '';

  search() {
    const q = this.q.trim();
    if (q) this.router.navigate(['/tim-kiem'], { queryParams: { q } });
  }

  logout() {
    this.auth.logout();
    this.router.navigateByUrl('/');
  }

  isStaff(): boolean {
    const roles = this.auth.user()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('Employee');
  }
}
