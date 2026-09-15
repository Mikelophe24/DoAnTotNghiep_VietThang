import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CatalogService } from '../../core/catalog.service';
import { CartService } from '../../core/cart.service';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/toast.service';
import { ProductDetail, ProductImage, VariantOption } from '../../core/models';
import { VndPipe } from '../../core/vnd.pipe';
import { ProductCardComponent } from '../../shared/product-card.component';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, VndPipe, ProductCardComponent],
  templateUrl: './product-detail.component.html'
})
export class ProductDetailComponent implements OnInit {
  private catalog = inject(CatalogService);
  private cart = inject(CartService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private http = inject(HttpClient);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private titleService = inject(Title);
  private sanitizer = inject(DomSanitizer);

  product = signal<ProductDetail | null>(null);
  notFound = signal(false);
  selectedColor = signal<number | null>(null);
  selectedSize = signal<number | null>(null);
  qty = signal(1);
  mainImage = signal<ProductImage | null>(null);
  adding = signal(false);
  descriptionHtml: SafeHtml | null = null;

  /** Biến thể đang chọn (màu + size). */
  variant = computed<VariantOption | null>(() => {
    const p = this.product();
    if (!p) return null;
    return p.variants.find(v => v.colorId === this.selectedColor() && v.sizeId === this.selectedSize()) ?? null;
  });
  canBuy = computed(() => (this.variant()?.stock ?? 0) > 0);
  price = computed(() => {
    const v = this.variant();
    const p = this.product();
    if (v) return { original: v.originalPrice, sale: v.salePrice, hasDiscount: v.salePrice < v.originalPrice, percent: Math.round((1 - v.salePrice / v.originalPrice) * 100) };
    return p ? { original: p.price.originalPrice, sale: p.price.salePrice, hasDiscount: p.price.hasDiscount, percent: p.price.discountPercent } : null;
  });
  stockInfo = computed(() => {
    const v = this.variant();
    if (v) return v.stock > 0 ? `Còn ${v.stock} sản phẩm · ${v.sku}` : 'Hết hàng';
    if (this.selectedColor() === null) return 'Chọn màu và kích cỡ';
    if (this.selectedSize() === null) return 'Chọn kích cỡ';
    return 'Không có biến thể này';
  });

  ngOnInit() {
    this.route.paramMap.subscribe(params => this.load(params.get('slug')!));
  }

  load(slug: string) {
    this.product.set(null);
    this.selectedColor.set(null);
    this.selectedSize.set(null);
    this.qty.set(1);
    this.catalog.product(slug).subscribe({
      next: p => {
        this.product.set(p);
        this.titleService.setTitle(`${p.name} - Thời trang Việt Thắng`);
        this.descriptionHtml = p.description ? this.sanitizer.bypassSecurityTrustHtml(p.description) : null;
        this.mainImage.set(p.images.find(i => i.isMain) ?? p.images[0] ?? null);
        const firstColor = p.colors.find(c => p.variants.some(v => v.colorId === c.id && v.stock > 0)) ?? p.colors[0];
        if (firstColor) this.selectColor(firstColor.id);
      },
      error: () => this.notFound.set(true)
    });
  }

  colorAvailable(colorId: number) { return this.product()!.variants.some(v => v.colorId === colorId && v.stock > 0); }
  sizeAvailable(sizeId: number) {
    const c = this.selectedColor();
    return this.product()!.variants.some(v => v.sizeId === sizeId && (c === null || v.colorId === c) && v.stock > 0);
  }
  colorName() { return this.product()?.colors.find(c => c.id === this.selectedColor())?.name ?? ''; }
  sizeName() { return this.product()?.sizes.find(s => s.id === this.selectedSize())?.name ?? ''; }

  selectColor(id: number) {
    this.selectedColor.set(id);
    const img = this.product()!.images.find(i => i.colorId === id);
    if (img) this.mainImage.set(img);
    if (this.selectedSize() !== null && !this.sizeAvailable(this.selectedSize()!)) this.selectedSize.set(null);
  }
  selectSize(id: number) { this.selectedSize.set(id); }
  changeQty(d: number) { this.qty.update(q => Math.max(1, q + d)); }
  setQty(e: Event) { this.qty.set(Math.max(1, Number((e.target as HTMLInputElement).value) || 1)); }

  async addToCart(buyNow = false) {
    const v = this.variant();
    if (!v || this.adding()) return;
    this.adding.set(true);
    const ok = await this.cart.add(v.id, this.qty());
    this.adding.set(false);
    if (ok && buyNow) this.router.navigateByUrl('/gio-hang');
  }

  toggleWishlist() {
    const p = this.product();
    if (!p) return;
    if (!this.auth.isLoggedIn()) { this.router.navigate(['/dang-nhap'], { queryParams: { returnUrl: this.router.url } }); return; }
    this.http.post<{ added: boolean; message: string }>(`/api/account/wishlist/${p.id}/toggle`, {}).subscribe(r => {
      this.product.set({ ...p, inWishlist: r.added });
      this.toast.success(r.message);
    });
  }

  stars(n: number) { return [1, 2, 3, 4, 5].map(i => i <= n); }
}
