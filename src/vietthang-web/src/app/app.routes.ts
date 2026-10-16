import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

/** Bảng định tuyến của storefront. Đường dẫn dùng tiếng Anh; slug danh mục/sản phẩm/bài viết lấy từ dữ liệu. */
export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent), title: 'Thời trang Việt Thắng' },

  { path: 'category/:slug', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'category' } },
  { path: 'search', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'search' }, title: 'Tìm kiếm' },
  { path: 'new-arrivals', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'new' }, title: 'Hàng mới về' },
  { path: 'sale', loadComponent: () => import('./pages/product-list/product-list.component').then(m => m.ProductListComponent), data: { mode: 'sale' }, title: 'Đang khuyến mãi' },
  { path: 'product/:slug', loadComponent: () => import('./pages/product-detail/product-detail.component').then(m => m.ProductDetailComponent) },

  { path: 'cart', loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent), title: 'Giỏ hàng' },
  { path: 'checkout', loadComponent: () => import('./pages/checkout/checkout.component').then(m => m.CheckoutComponent), title: 'Thanh toán' },
  { path: 'checkout/success/:code', loadComponent: () => import('./pages/order-success/order-success.component').then(m => m.OrderSuccessComponent), title: 'Đặt hàng thành công' },
  { path: 'order-lookup', loadComponent: () => import('./pages/order-lookup/order-lookup.component').then(m => m.OrderLookupComponent), title: 'Tra cứu đơn hàng' },

  { path: 'login', loadComponent: () => import('./pages/auth/login.component').then(m => m.LoginComponent), title: 'Đăng nhập' },
  { path: 'register', loadComponent: () => import('./pages/auth/register.component').then(m => m.RegisterComponent), title: 'Đăng ký' },
  { path: 'forgot-password', loadComponent: () => import('./pages/auth/forgot-password.component').then(m => m.ForgotPasswordComponent), title: 'Quên mật khẩu' },
  { path: 'reset-password', loadComponent: () => import('./pages/auth/reset-password.component').then(m => m.ResetPasswordComponent), title: 'Đặt lại mật khẩu' },

  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/account/account-layout.component').then(m => m.AccountLayoutComponent),
    children: [
      { path: '', loadComponent: () => import('./pages/account/profile.component').then(m => m.ProfileComponent), title: 'Hồ sơ của tôi' },
      { path: 'addresses', loadComponent: () => import('./pages/account/addresses.component').then(m => m.AddressesComponent), title: 'Sổ địa chỉ' },
      { path: 'orders', loadComponent: () => import('./pages/account/orders.component').then(m => m.OrdersComponent), title: 'Đơn hàng của tôi' },
      { path: 'orders/:code', loadComponent: () => import('./pages/account/order-detail.component').then(m => m.OrderDetailComponent), title: 'Chi tiết đơn hàng' },
      { path: 'wishlist', loadComponent: () => import('./pages/account/wishlist.component').then(m => m.WishlistComponent), title: 'Sản phẩm yêu thích' }
    ]
  },

  { path: 'news', loadComponent: () => import('./pages/posts/posts.component').then(m => m.PostsComponent), data: { type: 1 }, title: 'Tin tức' },
  { path: 'careers', loadComponent: () => import('./pages/posts/posts.component').then(m => m.PostsComponent), data: { type: 2 }, title: 'Tuyển dụng' },
  { path: 'news/:slug', loadComponent: () => import('./pages/posts/post-detail.component').then(m => m.PostDetailComponent) },
  { path: 'stores', loadComponent: () => import('./pages/stores/stores.component').then(m => m.StoresComponent), title: 'Hệ thống cửa hàng' },
  { path: 'contact', loadComponent: () => import('./pages/contact/contact.component').then(m => m.ContactComponent), title: 'Liên hệ' },

  { path: '**', loadComponent: () => import('./pages/not-found.component').then(m => m.NotFoundComponent), title: 'Không tìm thấy trang' }
];
