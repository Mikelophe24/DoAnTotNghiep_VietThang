import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <div class="container"><div class="row justify-content-center my-5"><div class="col-md-5 col-lg-4">
      <div class="card shadow-sm"><div class="card-body p-4">
        <h1 class="h4 mb-3 text-center">Đăng nhập</h1>
        @if (expired) { <div class="alert alert-warning py-2 small">Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.</div> }
        @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="mb-3"><label class="form-label">Email</label><input class="form-control" formControlName="email" autocomplete="username" [class.is-invalid]="invalid('email')" /><div class="invalid-feedback">Email không hợp lệ</div></div>
          <div class="mb-3"><label class="form-label">Mật khẩu</label><input type="password" class="form-control" formControlName="password" autocomplete="current-password" [class.is-invalid]="invalid('password')" /><div class="invalid-feedback">Nhập mật khẩu</div></div>
          <button class="btn btn-brand w-100" [disabled]="loading()">@if (loading()) { <span class="spinner-border spinner-border-sm"></span> } Đăng nhập</button>
        </form>
        <p class="text-center small mt-3 mb-0">Chưa có tài khoản? <a routerLink="/dang-ky" [queryParams]="{ returnUrl }">Đăng ký</a></p>
      </div></div>
    </div></div></div>
  `
})
export class LoginComponent implements OnInit {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  form = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]], password: ['', Validators.required] });
  error = signal<string | null>(null);
  loading = signal(false);
  returnUrl = '/';
  expired = false;

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.expired = this.route.snapshot.queryParamMap.has('expired');
    if (this.auth.isLoggedIn()) this.router.navigateByUrl(this.returnUrl);
  }

  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && c.touched; }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl),
      error: e => { this.error.set(e?.error?.message ?? 'Đăng nhập thất bại.'); this.loading.set(false); }
    });
  }
}
