import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../../core/cart.service';
import { VndPipe } from '../../core/vnd.pipe';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, FormsModule, VndPipe],
  templateUrl: './cart.component.html'
})
export class CartComponent implements OnInit {
  cart = inject(CartService);
  couponInput = '';

  ngOnInit() { this.cart.refresh(); }

  get hasUnavailable(): boolean { return (this.cart.summary()?.lines ?? []).some(l => !l.isAvailable); }

  setQty(variantId: number, e: Event) {
    const q = Number((e.target as HTMLInputElement).value) || 1;
    this.cart.update(variantId, q);
  }

  applyCoupon() {
    if (this.couponInput.trim()) this.cart.applyCoupon(this.couponInput);
  }

  removeCoupon() {
    this.cart.setCoupon(null);
    this.cart.refresh();
  }
}
