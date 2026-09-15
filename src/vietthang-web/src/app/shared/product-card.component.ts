import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductCard } from '../core/models';
import { VndPipe } from '../core/vnd.pipe';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [RouterLink, VndPipe],
  template: `
    <div class="product-card">
      <a class="product-thumb" [routerLink]="['/san-pham', p.slug]">
        <img [src]="p.imageUrl || '/images/products/placeholder.svg'" [alt]="p.name" loading="lazy" />
        <div class="product-badges">
          @if (p.hasDiscount) { <span class="badge-sale">-{{ p.discountPercent }}%</span> }
          @if (p.isNew) { <span class="badge-new">Mới</span> }
          @if (!p.inStock) { <span class="badge-out">Hết hàng</span> }
        </div>
      </a>
      <div class="product-info">
        @if (p.categoryName) { <div class="product-cat">{{ p.categoryName }}</div> }
        <a class="product-name" [routerLink]="['/san-pham', p.slug]">{{ p.name }}</a>
        <div class="product-price">
          <span class="price-sale">{{ p.salePrice | vnd }}</span>
          @if (p.hasDiscount) { <span class="price-original">{{ p.originalPrice | vnd }}</span> }
        </div>
      </div>
    </div>
  `
})
export class ProductCardComponent {
  @Input({ required: true }) p!: ProductCard;
}
