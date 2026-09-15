import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { combineLatest } from 'rxjs';
import { CatalogService } from '../../core/catalog.service';
import { Filters, ProductFilter, ProductListResponse } from '../../core/models';
import { ProductCardComponent } from '../../shared/product-card.component';
import { PaginationComponent } from '../../shared/pagination.component';

type Mode = 'category' | 'search' | 'new' | 'sale';

/** Trang danh mục / tìm kiếm / hàng mới / sale dùng chung, bộ lọc nằm trên query string. */
@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, FormsModule, ProductCardComponent, PaginationComponent],
  templateUrl: './product-list.component.html'
})
export class ProductListComponent implements OnInit {
  private catalog = inject(CatalogService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private titleService = inject(Title);

  mode: Mode = 'category';
  slug: string | null = null;
  filter: ProductFilter = { colorIds: [], sizeIds: [], sort: 'newest', page: 1 };
  filters = signal<Filters | null>(null);
  data = signal<ProductListResponse | null>(null);
  loading = signal(true);
  notFound = signal(false);

  readonly priceRanges = [
    { label: 'Dưới 200.000 đ', min: null, max: 199999 },
    { label: '200.000 – 300.000 đ', min: 200000, max: 300000 },
    { label: '300.000 – 400.000 đ', min: 300000, max: 400000 },
    { label: 'Trên 400.000 đ', min: 400001, max: null }
  ];

  ngOnInit() {
    this.catalog.filters().subscribe(f => this.filters.set(f));
    combineLatest([this.route.data, this.route.paramMap, this.route.queryParamMap]).subscribe(([data, params, query]) => {
      this.mode = data['mode'] as Mode;
      this.slug = params.get('slug');
      this.filter = {
        q: query.get('q') ?? undefined,
        colorIds: query.getAll('colorIds').map(Number),
        sizeIds: query.getAll('sizeIds').map(Number),
        minPrice: query.get('minPrice') ? Number(query.get('minPrice')) : null,
        maxPrice: query.get('maxPrice') ? Number(query.get('maxPrice')) : null,
        material: query.get('material'),
        sort: query.get('sort') ?? 'newest',
        page: Number(query.get('page') ?? 1)
      };
      this.load();
    });
  }

  get title(): string {
    switch (this.mode) {
      case 'category': return this.data()?.category?.name ?? '';
      case 'search': return this.filter.q ? `Kết quả cho "${this.filter.q}"` : 'Tìm kiếm';
      case 'new': return 'Hàng mới về';
      case 'sale': return 'Đang khuyến mãi';
    }
  }

  get hasFilter(): boolean {
    const f = this.filter;
    return f.colorIds.length > 0 || f.sizeIds.length > 0 || f.minPrice != null || f.maxPrice != null || !!f.material;
  }

  load() {
    this.loading.set(true);
    this.notFound.set(false);
    this.catalog.list(this.mode, this.slug, this.filter).subscribe({
      next: d => {
        this.data.set(d);
        this.loading.set(false);
        if (this.mode === 'category' && d.category) this.titleService.setTitle(`${d.category.name} - Thời trang Việt Thắng`);
      },
      error: () => { this.loading.set(false); this.notFound.set(true); }
    });
  }

  toggleId(list: number[], id: number) {
    const i = list.indexOf(id);
    if (i >= 0) list.splice(i, 1); else list.push(id);
  }

  isPriceRange(r: { min: number | null; max: number | null }) {
    return this.filter.minPrice === r.min && this.filter.maxPrice === r.max;
  }
  setPriceRange(r: { min: number | null; max: number | null }) {
    if (this.isPriceRange(r)) { this.filter.minPrice = null; this.filter.maxPrice = null; }
    else { this.filter.minPrice = r.min; this.filter.maxPrice = r.max; }
  }

  apply(page = 1) {
    const f = this.filter;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        q: f.q || null, colorIds: f.colorIds.length ? f.colorIds : null, sizeIds: f.sizeIds.length ? f.sizeIds : null,
        minPrice: f.minPrice ?? null, maxPrice: f.maxPrice ?? null, material: f.material || null,
        sort: f.sort !== 'newest' ? f.sort : null, page: page > 1 ? page : null
      }
    });
  }

  clear() {
    this.filter = { q: this.filter.q, colorIds: [], sizeIds: [], sort: 'newest', page: 1 };
    this.apply();
  }
}
