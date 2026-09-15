import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../core/toast.service';
import { Address } from '../../core/models';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-3">
      <h1 class="h4 mb-0">Sổ địa chỉ</h1>
      <button class="btn btn-brand" (click)="startNew()"><i class="bi bi-plus-lg"></i> Thêm địa chỉ</button>
    </div>

    @if (editing()) {
      <form [formGroup]="form" (ngSubmit)="save()" class="bg-white border rounded p-4 mb-4" novalidate>
        <h2 class="h6 mb-3">{{ form.value.id ? 'Sửa địa chỉ' : 'Thêm địa chỉ' }}</h2>
        <div class="row">
          <div class="col-md-6 mb-3"><label class="form-label required">Người nhận</label><input class="form-control" formControlName="receiverName" [class.is-invalid]="invalid('receiverName')" /></div>
          <div class="col-md-6 mb-3"><label class="form-label required">Số điện thoại</label><input class="form-control" formControlName="phone" [class.is-invalid]="invalid('phone')" /></div>
        </div>
        <div class="row">
          <div class="col-md-4 mb-3"><label class="form-label required">Tỉnh / Thành phố</label><input class="form-control" formControlName="province" [class.is-invalid]="invalid('province')" /></div>
          <div class="col-md-4 mb-3"><label class="form-label required">Quận / Huyện</label><input class="form-control" formControlName="district" [class.is-invalid]="invalid('district')" /></div>
          <div class="col-md-4 mb-3"><label class="form-label required">Phường / Xã</label><input class="form-control" formControlName="ward" [class.is-invalid]="invalid('ward')" /></div>
        </div>
        <div class="mb-3"><label class="form-label required">Số nhà, tên đường</label><input class="form-control" formControlName="street" [class.is-invalid]="invalid('street')" /></div>
        <div class="form-check mb-3"><input type="checkbox" class="form-check-input" id="isDefault" formControlName="isDefault" /><label class="form-check-label" for="isDefault">Đặt làm địa chỉ mặc định</label></div>
        <div class="d-flex gap-2"><button class="btn btn-brand"><i class="bi bi-check-lg"></i> Lưu địa chỉ</button><button type="button" class="btn btn-outline-secondary" (click)="editing.set(false)">Hủy</button></div>
      </form>
    }

    @if (!list().length) { <p class="text-muted">Bạn chưa lưu địa chỉ nào.</p> }
    <div class="row g-3">
      @for (a of list(); track a.id) {
        <div class="col-md-6">
          <div class="bg-white border rounded p-3 h-100" [class.border-primary]="a.isDefault">
            <div class="d-flex justify-content-between"><strong>{{ a.receiverName }}</strong>@if (a.isDefault) { <span class="badge bg-primary">Mặc định</span> }</div>
            <div class="small">{{ a.phone }}</div>
            <div class="small text-muted">{{ a.street }}, {{ a.ward }}, {{ a.district }}, {{ a.province }}</div>
            <div class="d-flex gap-2 mt-2">
              <button class="btn btn-sm btn-outline-secondary" (click)="edit(a)">Sửa</button>
              @if (!a.isDefault) { <button class="btn btn-sm btn-outline-primary" (click)="setDefault(a)">Đặt mặc định</button> }
              <button class="btn btn-sm btn-outline-danger" (click)="remove(a)">Xóa</button>
            </div>
          </div>
        </div>
      }
    </div>
  `
})
export class AddressesComponent implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);
  list = signal<Address[]>([]);
  editing = signal(false);
  form = this.fb.nonNullable.group({
    id: [0],
    receiverName: ['', Validators.required],
    phone: ['', [Validators.required, Validators.pattern(/^(0|\+84)\d{9,10}$/)]],
    province: ['', Validators.required], district: ['', Validators.required], ward: ['', Validators.required], street: ['', Validators.required],
    isDefault: [false]
  });

  ngOnInit() { this.load(); }
  load() { this.http.get<Address[]>('/api/account/addresses').subscribe(a => this.list.set(a)); }
  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && c.touched; }

  startNew() { this.form.reset({ id: 0, isDefault: this.list().length === 0 }); this.editing.set(true); }
  edit(a: Address) { this.form.reset({ ...a }); this.editing.set(true); }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    const req = v.id ? this.http.put<{ message: string }>(`/api/account/addresses/${v.id}`, v) : this.http.post<{ message: string }>('/api/account/addresses', v);
    req.subscribe({ next: r => { this.toast.success(r.message); this.editing.set(false); this.load(); }, error: e => this.toast.error(e?.error?.message ?? 'Không lưu được.') });
  }
  setDefault(a: Address) { this.http.post(`/api/account/addresses/${a.id}/default`, {}).subscribe(() => this.load()); }
  remove(a: Address) { if (confirm('Xóa địa chỉ này?')) this.http.delete(`/api/account/addresses/${a.id}`).subscribe(() => this.load()); }
}
