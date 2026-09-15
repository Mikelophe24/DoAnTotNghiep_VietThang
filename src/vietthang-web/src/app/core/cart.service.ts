import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';
import { CartItemLocal, CartSummary } from './models';

const CART_KEY = 'vt_cart';
const COUPON_KEY = 'vt_coupon';

/**
 * Giỏ hàng:
 *  - Khách vãng lai: danh sách {variantId, quantity} lưu localStorage, giá tính qua POST /api/cart/quote.
 *  - Khách đăng nhập: giỏ trong CSDL qua /api/cart; khi đăng nhập giỏ local được gộp lên server.
 */
@Injectable({ providedIn: 'root' })
export class CartService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  readonly localItems = signal<CartItemLocal[]>(readLocal());
  readonly coupon = signal<string | null>(localStorage.getItem(COUPON_KEY));
  readonly summary = signal<CartSummary | null>(null);
  readonly loading = signal(false);
  readonly count = computed(() => this.summary()?.totalQuantity ?? this.localItems().reduce((s, i) => s + i.quantity, 0));

  constructor() {
    // Mỗi khi trạng thái đăng nhập đổi thì tải lại giỏ (gộp giỏ local nếu vừa đăng nhập)
    effect(() => {
      const loggedIn = this.auth.isLoggedIn();
      untracked(() => loggedIn ? this.mergeLocalToServer().then(() => this.refresh()) : this.refresh());
    });
  }

  async refresh(): Promise<CartSummary | null> {
    this.loading.set(true);
    try {
      let s: CartSummary;
      if (this.auth.isLoggedIn()) {
        s = await firstValueFrom(this.http.get<CartSummary>('/api/cart', { params: this.coupon() ? { couponCode: this.coupon()! } : {} }));
      } else if (this.localItems().length === 0) {
        s = emptySummary();
      } else {
        s = await firstValueFrom(this.http.post<CartSummary>('/api/cart/quote', { items: this.localItems(), couponCode: this.coupon() }));
      }
      this.summary.set(s);
      return s;
    } catch {
      return this.summary();
    } finally {
      this.loading.set(false);
    }
  }

  async add(variantId: number, quantity = 1): Promise<boolean> {
    try {
      if (this.auth.isLoggedIn()) {
        const r = await firstValueFrom(this.http.post<{ message: string; count: number }>('/api/cart/items', { variantId, quantity }));
        this.toast.success(r.message);
      } else {
        const items = [...this.localItems()];
        const idx = items.findIndex(i => i.variantId === variantId);
        if (idx >= 0) items[idx] = { variantId, quantity: items[idx].quantity + quantity };
        else items.push({ variantId, quantity });
        this.writeLocal(items);
        this.toast.success('Đã thêm vào giỏ hàng.');
      }
      await this.refresh();
      return true;
    } catch (e: any) {
      this.toast.error(e?.error?.message ?? 'Không thể thêm vào giỏ, vui lòng thử lại.');
      return false;
    }
  }

  async update(variantId: number, quantity: number) {
    if (quantity <= 0) return this.remove(variantId);
    if (this.auth.isLoggedIn()) {
      await firstValueFrom(this.http.put(`/api/cart/items/${variantId}`, { variantId, quantity }));
    } else {
      this.writeLocal(this.localItems().map(i => i.variantId === variantId ? { variantId, quantity } : i));
    }
    await this.refresh();
  }

  async remove(variantId: number) {
    if (this.auth.isLoggedIn()) await firstValueFrom(this.http.delete(`/api/cart/items/${variantId}`));
    else this.writeLocal(this.localItems().filter(i => i.variantId !== variantId));
    await this.refresh();
  }

  async clear() {
    if (this.auth.isLoggedIn()) await firstValueFrom(this.http.delete('/api/cart')).catch(() => undefined);
    this.writeLocal([]);
    this.setCoupon(null);
    await this.refresh();
  }

  /** Sau khi đặt hàng thành công: server đã xóa giỏ CSDL, chỉ cần dọn local. */
  async afterCheckout() {
    this.writeLocal([]);
    this.setCoupon(null);
    await this.refresh();
  }

  setCoupon(code: string | null) {
    const v = code?.trim().toUpperCase() || null;
    if (v) localStorage.setItem(COUPON_KEY, v); else localStorage.removeItem(COUPON_KEY);
    this.coupon.set(v);
  }

  async applyCoupon(code: string) {
    this.setCoupon(code);
    const s = await this.refresh();
    if (s?.couponError) { this.toast.error(s.couponError); this.setCoupon(null); await this.refresh(); }
    else if (s?.coupon) this.toast.success(`Đã áp dụng mã ${s.coupon.code}, giảm ${s.discountAmount.toLocaleString('vi-VN')} đ.`);
  }

  private async mergeLocalToServer() {
    const items = this.localItems();
    if (items.length === 0) return;
    try {
      await firstValueFrom(this.http.post('/api/cart/merge', items));
      this.writeLocal([]);
    } catch { /* giữ giỏ local nếu gộp thất bại */ }
  }

  private writeLocal(items: CartItemLocal[]) {
    localStorage.setItem(CART_KEY, JSON.stringify(items));
    this.localItems.set(items);
  }
}

function readLocal(): CartItemLocal[] {
  try { const raw = localStorage.getItem(CART_KEY); return raw ? JSON.parse(raw) as CartItemLocal[] : []; }
  catch { return []; }
}

function emptySummary(): CartSummary {
  return { lines: [], subTotal: 0, totalQuantity: 0, discountAmount: 0, shippingFee: 0, freeShippingThreshold: 500000, total: 0, isEmpty: true };
}
