import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    <div class="container"><div class="row justify-content-center my-5"><div class="col-md-5 col-lg-4">
      <div class="card shadow-sm"><div class="card-body p-4">
        <h1 class="h4 mb-2 text-center">Quên mật khẩu</h1>
        @if (sent()) {
          <div class="alert alert-success small">{{ sent() }}</div>
          <a class="btn btn-outline-secondary w-100" routerLink="/login">Quay lại đăng nhập</a>
        } @else {
          <p class="text-muted small text-center">Nhập email đã đăng ký, chúng tôi sẽ gửi liên kết để bạn đặt mật khẩu mới.</p>
          @if (error()) { <div class="alert alert-danger py-2 small">{{ error() }}</div> }
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="mb-3">
              <label class="form-label">Email</label>
              <input class="form-control" formControlName="email" autocomplete="username" [class.is-invalid]="form.controls.email.invalid && form.controls.email.touched" />
              <div class="invalid-feedback">Email không hợp lệ</div>
            </div>
            <button class="btn btn-brand w-100" [disabled]="loading()">@if (loading()) { <span class="spinner-border spinner-border-sm"></span> } Gửi liên kết đặt lại</button>
          </form>
          <p class="text-center small mt-3 mb-0"><a routerLink="/login">Quay lại đăng nhập</a></p>
        }
      </div></div>
    </div></div></div>
  `
})
export class ForgotPasswordComponent {
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);
  form = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]] });
  loading = signal(false);
  sent = signal<string | null>(null);
  error = signal<string | null>(null);

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true); this.error.set(null);
    this.auth.forgotPassword(this.form.getRawValue().email).subscribe({
      next: r => { this.sent.set(r.message); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.message ?? 'Không gửi được yêu cầu, vui lòng thử lại.'); this.loading.set(false); }
    });
  }
}
