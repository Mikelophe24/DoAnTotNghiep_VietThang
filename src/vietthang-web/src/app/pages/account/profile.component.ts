import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { Profile } from '../../core/models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <h1 class="h4 mb-3">Hồ sơ của tôi</h1>
    <form [formGroup]="form" (ngSubmit)="save()" class="bg-white border rounded p-4 mb-4" style="max-width:640px" novalidate>
      <div class="mb-3"><label class="form-label required">Họ và tên</label><input class="form-control" formControlName="fullName" [class.is-invalid]="invalid(form, 'fullName')" /><div class="invalid-feedback">Nhập họ tên</div></div>
      <div class="mb-3"><label class="form-label">Email</label><input class="form-control" formControlName="email" readonly /></div>
      <div class="mb-3"><label class="form-label required">Số điện thoại</label><input class="form-control" formControlName="phoneNumber" [class.is-invalid]="invalid(form, 'phoneNumber')" /><div class="invalid-feedback">Số điện thoại không hợp lệ</div></div>
      <div class="row">
        <div class="col-md-6 mb-3"><label class="form-label">Giới tính</label><select class="form-select" formControlName="gender"><option [ngValue]="null">— Chọn —</option><option [ngValue]="2">Nữ</option><option [ngValue]="1">Nam</option><option [ngValue]="0">Khác</option></select></div>
        <div class="col-md-6 mb-3"><label class="form-label">Ngày sinh</label><input type="date" class="form-control" formControlName="dateOfBirth" /></div>
      </div>
      <button class="btn btn-brand" [disabled]="saving()"><i class="bi bi-check-lg"></i> Lưu thay đổi</button>
    </form>

    <h2 class="h5 mb-3">Đổi mật khẩu</h2>
    <form [formGroup]="pwForm" (ngSubmit)="changePassword()" class="bg-white border rounded p-4" style="max-width:480px" novalidate>
      <div class="mb-3"><label class="form-label">Mật khẩu hiện tại</label><input type="password" class="form-control" formControlName="currentPassword" [class.is-invalid]="invalid(pwForm, 'currentPassword')" /></div>
      <div class="mb-3"><label class="form-label">Mật khẩu mới</label><input type="password" class="form-control" formControlName="newPassword" [class.is-invalid]="invalid(pwForm, 'newPassword')" /><div class="invalid-feedback">Tối thiểu 8 ký tự</div></div>
      <div class="mb-3"><label class="form-label">Nhập lại mật khẩu mới</label><input type="password" class="form-control" formControlName="confirmPassword" [class.is-invalid]="pwForm.value.confirmPassword !== pwForm.value.newPassword && pwForm.get('confirmPassword')!.touched" /><div class="invalid-feedback">Mật khẩu nhập lại không khớp</div></div>
      <button class="btn btn-outline-dark" [disabled]="saving()"><i class="bi bi-key"></i> Đổi mật khẩu</button>
    </form>
  `
})
export class ProfileComponent implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  saving = signal(false);

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: [''],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^(0|\+84)\d{9,10}$/)]],
    gender: [null as number | null],
    dateOfBirth: [null as string | null]
  });
  pwForm = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  });

  ngOnInit() {
    this.http.get<Profile>('/api/account/profile').subscribe(p => this.form.patchValue({
      fullName: p.fullName, email: p.email ?? '', phoneNumber: p.phoneNumber ?? '', gender: p.gender ?? null, dateOfBirth: p.dateOfBirth ?? null
    }));
  }

  invalid(form: any, name: string) { const c = form.get(name); return !!c && c.invalid && c.touched; }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    const v = this.form.value;
    this.http.put<{ message: string }>('/api/account/profile', { fullName: v.fullName, phoneNumber: v.phoneNumber, gender: v.gender, dateOfBirth: v.dateOfBirth || null }).subscribe({
      next: r => { this.toast.success(r.message); this.auth.patchUser({ fullName: v.fullName!, phone: v.phoneNumber }); this.saving.set(false); },
      error: e => { this.toast.error(e?.error?.message ?? 'Không lưu được.'); this.saving.set(false); }
    });
  }

  changePassword() {
    const v = this.pwForm.getRawValue();
    if (this.pwForm.invalid || v.newPassword !== v.confirmPassword) { this.pwForm.markAllAsTouched(); return; }
    this.saving.set(true);
    this.http.post<{ message: string }>('/api/account/change-password', v).subscribe({
      next: r => { this.toast.success(r.message); this.pwForm.reset(); this.saving.set(false); },
      error: e => { this.toast.error(e?.error?.message ?? 'Không đổi được mật khẩu.'); this.saving.set(false); }
    });
  }
}
