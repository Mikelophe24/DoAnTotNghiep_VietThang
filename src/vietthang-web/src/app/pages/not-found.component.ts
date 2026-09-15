import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container text-center my-5">
      <h1 class="display-6">404</h1>
      <p class="lead">Trang bạn tìm không tồn tại.</p>
      <a class="btn btn-brand" routerLink="/">Về trang chủ</a>
    </div>
  `
})
export class NotFoundComponent {}
