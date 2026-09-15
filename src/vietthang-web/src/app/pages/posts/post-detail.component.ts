import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeHtml, Title } from '@angular/platform-browser';
import { CatalogService } from '../../core/catalog.service';
import { PostDetail } from '../../core/models';

@Component({
  selector: 'app-post-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <div class="container mt-3">
      @if (post(); as p) {
        <nav aria-label="breadcrumb"><ol class="breadcrumb small">
          <li class="breadcrumb-item"><a routerLink="/">Trang chủ</a></li>
          <li class="breadcrumb-item"><a [routerLink]="p.type === 2 ? '/tuyen-dung' : '/tin-tuc'">{{ p.type === 2 ? 'Tuyển dụng' : 'Tin tức' }}</a></li>
          <li class="breadcrumb-item active">{{ p.title }}</li>
        </ol></nav>
        <div class="row g-4">
          <article class="col-lg-8">
            <h1 class="h3">{{ p.title }}</h1>
            <div class="small text-muted mb-3">{{ p.publishedAt | date:'dd/MM/yyyy' }} {{ p.author ? '· ' + p.author : '' }}</div>
            @if (p.thumbnailUrl) { <img [src]="p.thumbnailUrl" class="img-fluid rounded mb-3" alt="" /> }
            <div class="post-content bg-white border rounded p-4" [innerHTML]="content"></div>
          </article>
          <aside class="col-lg-4">
            <h2 class="h6">Bài viết khác</h2>
            <ul class="list-unstyled">@for (r of p.recent; track r.slug) { <li class="mb-2"><a [routerLink]="['/tin-tuc', r.slug]">{{ r.title }}</a></li> }</ul>
          </aside>
        </div>
      } @else if (notFound()) { <div class="alert alert-warning">Không tìm thấy bài viết.</div> }
      @else { <div class="skeleton" style="height:300px"></div> }
    </div>
  `
})
export class PostDetailComponent implements OnInit {
  private catalog = inject(CatalogService);
  private route = inject(ActivatedRoute);
  private sanitizer = inject(DomSanitizer);
  private titleService = inject(Title);
  post = signal<PostDetail | null>(null);
  notFound = signal(false);
  content: SafeHtml = '';

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.post.set(null);
      this.catalog.post(params.get('slug')!).subscribe({
        next: p => { this.post.set(p); this.content = this.sanitizer.bypassSecurityTrustHtml(p.content); this.titleService.setTitle(`${p.title} - Thời trang Việt Thắng`); },
        error: () => this.notFound.set(true)
      });
    });
  }
}
