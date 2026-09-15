import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CatalogService } from '../../core/catalog.service';
import { HomeData } from '../../core/models';
import { ProductCardComponent } from '../../shared/product-card.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, ProductCardComponent, DatePipe, DecimalPipe],
  templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
  private catalog = inject(CatalogService);
  data = signal<HomeData | null>(null);
  error = signal<string | null>(null);

  ngOnInit() {
    this.catalog.home().subscribe({
      next: d => this.data.set(d),
      error: () => this.error.set('Không tải được dữ liệu. Kiểm tra backend đã chạy ở cổng 5292 chưa.')
    });
  }
}
