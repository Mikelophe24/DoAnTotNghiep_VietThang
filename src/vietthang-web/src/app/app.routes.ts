import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

/** Đường dẫn tiếng Việt, giữ giống phiên bản Razor để tài liệu và SEO nhất quán. */
export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent), title: 'Thời trang Việt Thắng' },

  { path: 'danh-muc/:slug', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'category' } },
  { path: 'tim-kiem', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'search' }, title: 'Tìm kiếm' },
  { path: 'hang-moi-ve', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'new' }, title: 'Hàng mới về' },
  { path: 'sale', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'sale' }, title: 'Đang khuyến mãi' },
  { path: 'san-pham/:slug', loadComponent: () => import('./pages/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },

  { path: 'gio-hang', loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent), title: 'Giỏ hàng' },
  { path: 'thanh-toan', loadComponent: () => import('./pages/checkout/checkout.component').then(m => m.CheckoutComponent), title: 'Thanh toán' },
  { path: 'thanh-toan/thanh-cong/:code', loadComponent: () => import('./pages/order-success/order-success.component').then(m => m.OrderSuccessComponent), title: 'Đặt hàng thành công' },
  { path: 'tra-cuu-don-hang', loadComponent: () => import('./pages/order-lookup/order-lookup.component').then(m => m.OrderLookupComponent), title: 'Tra cứu đơn hàng' },

  { path: 'dang-nhap', loadComponent: () => import('./pages/auth/login.component').then(m => m.LoginComponent), title: 'Đăng nhập' },
  { path: 'dang-ky', loadComponent: () => import('./pages/auth/register.component').then(m => m.RegisterComponent), title: 'Đăng ký' },

  {
    path: 'tai-khoan',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/account/account-layout.component').then(m => m.AccountLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./pages/account/profile.component').then(m => m.ProfileComponent), title: 'Hồ sơ của tôi' },
      { path: 'dia-chi', loadComponent: () => import('./pages/account/addresses.component').then(m => m.AddressesComponent), title: 'Sổ địa chỉ' },
      { path: 'don-hang', loadComponent: () => import('./pages/account/orders.component').then(m => m.OrdersComponent), title: 'Đơn hàng của tôi' },
      { path: 'don-hang/:code', loadComponent: () => import('./pages/account/order-detail.component').then(m => m.OrderDetailComponent), title: 'Chi tiết đơn hàng' },
      { path: 'yeu-thich', loadComponent: () => import('./pages/account/wishlist.component').then(m => m.WishlistComponent), title: 'Sản phẩm yêu thích' }
    ]
  },

  { path: 'tin-tuc', loadComponent: () => import('./pages/posts/posts.component').then(m => m.PostsComponent), data: { type: 1 }, title: 'Tin tức' },
  { path: 'tuyen-dung', loadComponent: () => import('./pages/posts/posts.component').then(m => m.PostsComponent), data: { type: 2 }, title: 'Tuyển dụng' },
  { path: 'tin-tuc/:slug', loadComponent: () => import('./pages/posts/post-detail.component').then(m => m.PostDetailComponent) },
  { path: 'he-thong-cua-hang', loadComponent: () => import('./pages/stores/stores.component').then(m => m.StoresComponent), title: 'Hệ thống cửa hàng' },
  { path: 'lien-he', loadComponent: () => import('./pages/contact/contact.component').then(m => m.ContactComponent), title: 'Liên hệ' },

  { path: '**', loadComponent: () => import('./pages/not-found.component').then(m => m.NotFoundComponent), title: 'Không tìm thấy trang' }
];
