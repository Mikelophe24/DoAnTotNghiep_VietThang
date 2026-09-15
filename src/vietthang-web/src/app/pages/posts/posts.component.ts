import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogService } from '../../core/catalog.service';
import { Paged, PostSummary } from '../../core/models';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-posts',
  standalone: true,
  imports: [RouterLink, DatePipe, PaginationComponent],
  template: `
    <div class="container mt-3">
      <h1 class="h4 mb-3">{{ type === 1 ? 'Tin tức' : 'Tuyển dụng' }}</h1>
      @if (data(); as d) {
        @if (!d.items.length) { <p class="text-muted">Chưa có bài viết.</p> }
        <div class="row g-3">
          @for (post of d.items; track post.id) {
            <div class="col-md-4">
              <a class="post-card" [routerLink]="['/tin-tuc', post.slug]">
                <img [src]="post.thumbnailUrl || '/images/banners/banner-1.svg'" [alt]="post.title" loading="lazy" />
                <div class="post-card-body">
                  <div class="small text-muted">{{ post.publishedAt | date:'dd/MM/yyyy' }}</div>
                  <h2 class="h6 mb-1">{{ post.title }}</h2>
                  <p class="small text-muted mb-0">{{ post.summary }}</p>
                </div>
              </a>
            </div>
          }
        </div>
        <app-pagination [page]="d.pageIndex" [totalPages]="d.totalPages" (pageChange)="load($event)" />
      } @else { <div class="skeleton" style="height:200px"></div> }
    </div>
  `
})
export class PostsComponent implements OnInit {
  private catalog = inject(CatalogService);
  private route = inject(ActivatedRoute);
  type = 1;
  data = signal<Paged<PostSummary> | null>(null);
  ngOnInit() { this.route.data.subscribe(d => { this.type = d['type'] ?? 1; this.load(1); }); }
  load(page: number) { this.data.set(null); this.catalog.posts(this.type, page).subscribe(d => this.data.set(d)); }
}
