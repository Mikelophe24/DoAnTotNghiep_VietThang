import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { Filters, HomeData, MenuCategory, Paged, PostDetail, PostSummary, ProductDetail, ProductFilter, ProductListResponse, Settings, Store } from './models';

/** Gọi API danh mục, sản phẩm, nội dung công khai. */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);
  private menu$?: Observable<MenuCategory[]>;
  private filters$?: Observable<Filters>;
  private settings$?: Observable<Settings>;

  menu(): Observable<MenuCategory[]> {
    return this.menu$ ??= this.http.get<MenuCategory[]>('/api/catalog/menu').pipe(shareReplay(1));
  }
  filters(): Observable<Filters> {
    return this.filters$ ??= this.http.get<Filters>('/api/catalog/filters').pipe(shareReplay(1));
  }
  settings(): Observable<Settings> {
    return this.settings$ ??= this.http.get<Settings>('/api/content/settings').pipe(shareReplay(1));
  }

  home(): Observable<HomeData> { return this.http.get<HomeData>('/api/catalog/home'); }

  list(mode: 'category' | 'search' | 'new' | 'sale', slug: string | null, filter: ProductFilter): Observable<ProductListResponse> {
    let params = new HttpParams().set('page', filter.page).set('sort', filter.sort ?? 'newest');
    if (filter.q) params = params.set('q', filter.q);
    if (filter.minPrice != null) params = params.set('minPrice', filter.minPrice);
    if (filter.maxPrice != null) params = params.set('maxPrice', filter.maxPrice);
    if (filter.material) params = params.set('material', filter.material);
    filter.colorIds.forEach(id => params = params.append('colorIds', id));
    filter.sizeIds.forEach(id => params = params.append('sizeIds', id));
    const url = mode === 'category' ? `/api/catalog/categories/${slug}` : `/api/catalog/${mode}`;
    return this.http.get<ProductListResponse>(url, { params });
  }

  product(slug: string): Observable<ProductDetail> { return this.http.get<ProductDetail>(`/api/catalog/products/${slug}`); }

  posts(type: number, page = 1): Observable<Paged<PostSummary>> {
    return this.http.get<Paged<PostSummary>>('/api/content/posts', { params: { type, page } });
  }
  post(slug: string): Observable<PostDetail> { return this.http.get<PostDetail>(`/api/content/posts/${slug}`); }
  stores(): Observable<Store[]> { return this.http.get<Store[]>('/api/content/stores'); }
  contact(data: { fullName: string; email?: string; phone?: string; subject?: string; message: string }) {
    return this.http.post<{ message: string }>('/api/content/contact', data);
  }
}
