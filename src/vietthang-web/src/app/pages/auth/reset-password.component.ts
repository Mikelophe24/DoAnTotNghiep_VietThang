import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';

/** Trang mở từ liên kết trong email: /reset-password?email=...&token=... */
@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <div class="container"><div class="row justify-content-center my-5"><div class="col-md-5 col-lg-4">
      <div class="card shadow-sm"><div class="card-body p-4">
        <h1 class="h4 mb-3 text-center">Đặt lại mật khẩu</h1>
        @if (!email || !token) {
          <div class="alert alert-warning small">Liên kết không hợp lệ. Vui lòng yêu cầu lại từ trang <a routerLink="/forgot-password">Quên mật khẩu</a>.</div>
        } @else {
          <p class="text-muted small text-center">Tài khoản: <strong>{{ email }}</strong></p>
          @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="mb-3">
              <label class="form-label">Mật khẩu mới</label>
              <input type="password" class="form-control" formControlName="password" autocomplete="new-password" [class.is-invalid]="form.controls.password.invalid && form.controls.password.touched" />
              <div class="invalid-feedback">Tối thiểu 8 ký tự</div>
            </div>
            <div class="mb-3">
              <label class="form-label">Nhập lại mật khẩu mới</label>
              <input type="password" class="form-control" formControlName="confirm" autocomplete="new-password" [class.is-invalid]="mismatch()" />
              <div class="invalid-feedback">Mật khẩu nhập lại không khớp</div>
            </div>
            <div class="form-text mb-3">Mật khẩu tối thiểu 8 ký tự, có chữ in hoa, chữ thường và số.</div>
            <button class="btn btn-brand w-100" [disabled]="loading()">@if (loading()) { <span class="spinner-border spinner-border-sm"></span> } Đặt mật khẩu mới</button>
          </form>
        }
      </div></div>
    </div></div></div>
  `
})
export class ResetPasswordComponent implements OnInit {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);

  email = '';
  token = '';
  form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', Validators.required]
  });
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    const q = this.route.snapshot.queryParamMap;
    this.email = q.get('email') ?? '';
    this.token = q.get('token') ?? '';
  }

  mismatch() { const v = this.form.getRawValue(); return this.form.controls.confirm.touched && v.password !== v.confirm; }

  submit() {
    const v = this.form.getRawValue();
    if (this.form.invalid || v.password !== v.confirm) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(null);
    this.auth.resetPassword(this.email, this.token, v.password).subscribe({
      next: r => { this.toast.success(r.message); this.router.navigateByUrl('/login'); },
      error: e => { this.error.set(e?.error?.message ?? 'Không đặt lại được mật khẩu.'); this.loading.set(false); }
    });
  }
}
