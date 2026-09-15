import { Component, inject, signal } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CatalogService } from '../../core/catalog.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, AsyncPipe],
  template: `
    <div class="container mt-3"><div class="row g-4">
      <div class="col-lg-7">
        <h1 class="h4 mb-3">Liên hệ với cửa hàng</h1>
        <form [formGroup]="form" (ngSubmit)="submit()" class="bg-white border rounded p-4" novalidate>
          <div class="row">
            <div class="col-md-6 mb-3"><label class="form-label required">Họ và tên</label><input class="form-control" formControlName="fullName" [class.is-invalid]="invalid('fullName')" /><div class="invalid-feedback">Nhập họ tên</div></div>
            <div class="col-md-6 mb-3"><label class="form-label">Số điện thoại</label><input class="form-control" formControlName="phone" /></div>
          </div>
          <div class="mb-3"><label class="form-label">Email</label><input class="form-control" formControlName="email" [class.is-invalid]="invalid('email')" /><div class="invalid-feedback">Email không hợp lệ</div></div>
          <div class="mb-3"><label class="form-label">Chủ đề</label><input class="form-control" formControlName="subject" /></div>
          <div class="mb-3"><label class="form-label required">Nội dung</label><textarea class="form-control" rows="5" formControlName="message" [class.is-invalid]="invalid('message')"></textarea><div class="invalid-feedback">Nhập nội dung</div></div>
          <button class="btn btn-brand" [disabled]="sending()"><i class="bi bi-send"></i> Gửi liên hệ</button>
        </form>
      </div>
      <div class="col-lg-5">
        <div class="bg-white border rounded p-4">
          <h2 class="h6">Thông tin liên hệ</h2>
          @if (settings$ | async; as s) {
            <p class="mb-1"><i class="bi bi-telephone"></i> Hotline: <a href="tel:{{ s.hotline }}">{{ s.hotline }}</a> (miễn phí)</p>
            <p class="mb-1"><i class="bi bi-envelope"></i> {{ s.email }}</p>
          }
          <p class="mb-0"><i class="bi bi-clock"></i> 8:30 – 21:30 hằng ngày</p>
        </div>
      </div>
    </div></div>
  `
})
export class ContactComponent {
  private catalog = inject(CatalogService);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);
  settings$ = this.catalog.settings();
  sending = signal(false);
  form = this.fb.nonNullable.group({
    fullName: ['', Validators.required], phone: [''], email: ['', Validators.email], subject: [''], message: ['', Validators.required]
  });
  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && c.touched; }
  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.sending.set(true);
    this.catalog.contact(this.form.getRawValue()).subscribe({
      next: r => { this.toast.success(r.message); this.form.reset(); this.sending.set(false); },
      error: e => { this.toast.error(e?.error?.message ?? 'Không gửi được, vui lòng thử lại.'); this.sending.set(false); }
    });
  }
}
