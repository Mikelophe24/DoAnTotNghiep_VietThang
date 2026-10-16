import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AddressProvince, AddressService } from '../core/address.service';

/**
 * Ba ô chọn liên cấp Tỉnh → Quận/Huyện → Phường/Xã, gắn vào FormGroup cha có các control
 * 'province', 'district', 'ward'. Giá trị cũ không có trong danh mục (địa chỉ nhập tay trước đây) vẫn được giữ.
 */
@Component({
  selector: 'app-address-select',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="row" [formGroup]="group">
      <div class="col-md-4 mb-3">
        <label class="form-label required">Tỉnh / Thành phố</label>
        <select class="form-select" formControlName="province" (change)="onProvinceChange()" [class.is-invalid]="invalid('province')">
          <option value="">— Chọn tỉnh / thành phố —</option>
          @for (name of provinces(); track name) { <option [value]="name">{{ name }}</option> }
        </select>
        <div class="invalid-feedback">Chọn tỉnh / thành phố</div>
      </div>
      <div class="col-md-4 mb-3">
        <label class="form-label required">Quận / Huyện</label>
        <select class="form-select" formControlName="district" (change)="onDistrictChange()" [class.is-invalid]="invalid('district')">
          <option value="">{{ value('province') ? '— Chọn quận / huyện —' : '— Chọn tỉnh trước —' }}</option>
          @for (name of districts(); track name) { <option [value]="name">{{ name }}</option> }
        </select>
        <div class="invalid-feedback">Chọn quận / huyện</div>
      </div>
      <div class="col-md-4 mb-3">
        <label class="form-label required">Phường / Xã</label>
        <select class="form-select" formControlName="ward" [class.is-invalid]="invalid('ward')">
          <option value="">{{ value('district') ? '— Chọn phường / xã —' : '— Chọn quận trước —' }}</option>
          @for (name of wards(); track name) { <option [value]="name">{{ name }}</option> }
        </select>
        <div class="invalid-feedback">Chọn phường / xã</div>
      </div>
    </div>
  `
})
export class AddressSelectComponent implements OnInit {
  @Input({ required: true }) group!: FormGroup;
  /** true khi form cha đã bấm submit, để hiện lỗi cả ở ô chưa chạm vào. */
  @Input() showErrors = false;

  private address = inject(AddressService);
  private data = signal<AddressProvince[]>([]);

  ngOnInit() { this.address.load().subscribe(d => this.data.set(d)); }

  value(name: string): string { return this.group.get(name)?.value ?? ''; }
  invalid(name: string): boolean { const c = this.group.get(name); return !!c && c.invalid && (c.touched || this.showErrors); }

  provinces(): string[] { return withCurrent(this.data().map(p => p.n), this.value('province')); }

  districts(): string[] {
    const p = this.data().find(x => x.n === this.value('province'));
    return withCurrent(p ? p.d.map(d => d.n) : [], this.value('district'));
  }

  wards(): string[] {
    const p = this.data().find(x => x.n === this.value('province'));
    const d = p?.d.find(x => x.n === this.value('district'));
    return withCurrent(d ? d.w : [], this.value('ward'));
  }

  onProvinceChange() { this.group.patchValue({ district: '', ward: '' }); }
  onDistrictChange() { this.group.patchValue({ ward: '' }); }
}

/** Thêm giá trị hiện tại vào danh sách nếu chưa có, để địa chỉ cũ nhập tay vẫn hiển thị được. */
function withCurrent(list: string[], current: string): string[] {
  return current && !list.includes(current) ? [current, ...list] : list;
}
