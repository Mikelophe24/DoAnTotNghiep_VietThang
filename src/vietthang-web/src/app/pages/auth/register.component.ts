import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';

function match(a: string, b: string) {
  return (group: AbstractControl) => group.get(a)?.value === group.get(b)?.value ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <div class="container"><div class="row justify-content-center my-5"><div class="col-md-6 col-lg-5">
      <div class="card shadow-sm"><div class="card-body p-4">
        <h1 class="h4 mb-3 text-center">Đăng ký tài khoản</h1>
        @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="mb-3"><label class="form-label">Họ và tên</label><input class="form-control" formControlName="fullName" [class.is-invalid]="invalid('fullName')" /><div class="invalid-feedback">Nhập họ tên</div></div>
          <div class="mb-3"><label class="form-label">Email</label><input class="form-control" formControlName="email" [class.is-invalid]="invalid('email')" /><div class="invalid-feedback">Email không hợp lệ</div></div>
          <div class="mb-3"><label class="form-label">Số điện thoại</label><input class="form-control" formControlName="phoneNumber" [class.is-invalid]="invalid('phoneNumber')" /><div class="invalid-feedback">Số điện thoại không hợp lệ</div></div>
          <div class="row">
            <div class="col-md-6 mb-3"><label class="form-label">Mật khẩu</label><input type="password" class="form-control" formControlName="password" [class.is-invalid]="invalid('password')" /><div class="invalid-feedback">Tối thiểu 8 ký tự</div></div>
            <div class="col-md-6 mb-3"><label class="form-label">Nhập lại mật khẩu</label><input type="password" class="form-control" formControlName="confirm" [class.is-invalid]="form.hasError('mismatch') && form.get('confirm')!.touched" /><div class="invalid-feedback">Mật khẩu nhập lại không khớp</div></div>
          </div>
          <div class="form-text mb-3">Mật khẩu tối thiểu 8 ký tự, có chữ in hoa, chữ thường và số.</div>
          <button class="btn btn-brand w-100" [disabled]="loading()">@if (loading()) { <span class="spinner-border spinner-border-sm"></span> } Đăng ký</button>
        </form>
        <p class="text-center small mt-3 mb-0">Đã có tài khoản? <a routerLink="/dang-nhap">Đăng nhập</a></p>
      </div></div>
    </div></div></div>
  `
})
export class RegisterComponent {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);
  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^(0|\+84)\d{9,10}$/)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', Validators.required]
  }, { validators: match('password', 'confirm') });
  error = signal<string | null>(null);
  loading = signal(false);

  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && c.touched; }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(null);
    const { confirm, ...data } = this.form.getRawValue();
    this.auth.register(data).subscribe({
      next: () => { this.toast.success('Đăng ký thành công. Chào mừng bạn đến với Thời trang Việt Thắng!'); this.router.navigateByUrl(this.route.snapshot.queryParamMap.get('returnUrl') || '/'); },
      error: e => { this.error.set(e?.error?.message ?? 'Đăng ký thất bại.'); this.loading.set(false); }
    });
  }
}
