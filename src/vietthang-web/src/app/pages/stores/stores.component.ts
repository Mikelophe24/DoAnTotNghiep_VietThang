import { Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { CatalogService } from '../../core/catalog.service';

@Component({
  selector: 'app-stores',
  standalone: true,
  imports: [AsyncPipe],
  template: `
    <div class="container mt-3">
      <h1 class="h4 mb-3">Hệ thống cửa hàng</h1>
      <div class="row g-3">
        @for (s of stores$ | async; track s.id) {
          <div class="col-md-6 col-lg-4">
            <div class="bg-white border rounded p-3 h-100">
              <h2 class="h6 text-brand"><i class="bi bi-geo-alt-fill"></i> {{ s.name }}</h2>
              <p class="mb-1">{{ s.address }}</p>
              @if (s.phone) { <p class="mb-1 small"><i class="bi bi-telephone"></i> <a href="tel:{{ s.phone }}">{{ s.phone }}</a></p> }
              @if (s.openingHours) { <p class="mb-1 small"><i class="bi bi-clock"></i> {{ s.openingHours }}</p> }
              <a class="small" [href]="'https://www.google.com/maps/search/?api=1&query=' + encode(s.address)" target="_blank" rel="noopener">Chỉ đường trên Google Maps →</a>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class StoresComponent {
  stores$ = inject(CatalogService).stores();
  encode = encodeURIComponent;
}
