import { Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { CartService } from '../../core/cart.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { CatalogService } from '../../core/catalog.service';
import { Address, CheckoutPayload, CheckoutResult, Profile } from '../../core/models';
import { VndPipe } from '../../core/vnd.pipe';

const PHONE = /^(0|\+84)\d{9,10}$/;

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, VndPipe],
  templateUrl: './checkout.component.html'
})
export class CheckoutComponent implements OnInit {
  cart = inject(CartService);
  auth = inject(AuthService);
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private router = inject(Router);
  private toast = inject(ToastService);
  private catalog = inject(CatalogService);

  addresses = signal<Address[]>([]);
  bankAccount = signal<string | null>(null);
  submitting = signal(false);
  submitted = signal(false);

  form = this.fb.nonNullable.group({
    customerName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', [Validators.required, Validators.pattern(PHONE)]],
    email: ['', [Validators.email]],
    province: ['', Validators.required],
    district: ['', Validators.required],
    ward: ['', Validators.required],
    street: ['', Validators.required],
    note: [''],
    paymentMethod: [1, Validators.required],
    saveAddress: [false]
  });

  async ngOnInit() {
    const s = await this.cart.refresh();
    if (!s || s.isEmpty) { this.toast.error('Giỏ hàng của bạn đang trống.'); this.router.navigateByUrl('/gio-hang'); return; }
    if (s.lines.some(l => !l.isAvailable)) { this.toast.error('Một số sản phẩm trong giỏ không đủ hàng, vui lòng cập nhật số lượng.'); this.router.navigateByUrl('/gio-hang'); return; }
    this.catalog.settings().subscribe(st => this.bankAccount.set(st.bankAccount ?? null));

    if (this.auth.isLoggedIn()) {
      const [profile, addresses] = await Promise.all([
        firstValueFrom(this.http.get<Profile>('/api/account/profile')).catch(() => null),
        firstValueFrom(this.http.get<Address[]>('/api/account/addresses')).catch(() => [] as Address[])
      ]);
      if (profile) this.form.patchValue({ customerName: profile.fullName, phone: profile.phoneNumber ?? '', email: profile.email ?? '' });
      this.addresses.set(addresses);
      const def = addresses[0];
      if (def) this.useAddress(def);
    }
  }

  useAddress(a: Address) {
    this.form.patchValue({ customerName: a.receiverName, phone: a.phone, province: a.province, district: a.district, ward: a.ward, street: a.street });
  }
  onAddressSelect(e: Event) {
    const id = Number((e.target as HTMLSelectElement).value);
    const a = this.addresses().find(x => x.id === id);
    if (a) this.useAddress(a);
  }

  setPayment(m: number) { this.form.patchValue({ paymentMethod: m }); }
  invalid(name: string) { const c = this.form.get(name); return !!c && c.invalid && (c.touched || this.submitted()); }

  async submit() {
    this.submitted.set(true);
    if (this.form.invalid || this.submitting()) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    const v = this.form.getRawValue();
    const payload: CheckoutPayload = {
      ...v, email: v.email || null, note: v.note || null, paymentMethod: Number(v.paymentMethod),
      couponCode: this.cart.coupon(),
      items: this.auth.isLoggedIn() ? undefined : this.cart.localItems()
    };
    try {
      const r = await firstValueFrom(this.http.post<CheckoutResult>('/api/checkout', payload));
      await this.cart.afterCheckout();
      this.router.navigate(['/thanh-toan/thanh-cong', r.orderCode], { state: { result: r } });
    } catch (e: any) {
      this.toast.error(e?.error?.message ?? 'Đặt hàng thất bại, vui lòng thử lại.');
      await this.cart.refresh();
    } finally {
      this.submitting.set(false);
    }
  }
}
