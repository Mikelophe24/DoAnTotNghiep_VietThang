import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Chặn các trang tài khoản khi chưa đăng nhập, nhớ returnUrl để quay lại. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  return router.createUrlTree(['/dang-nhap'], { queryParams: { returnUrl: state.url } });
};
