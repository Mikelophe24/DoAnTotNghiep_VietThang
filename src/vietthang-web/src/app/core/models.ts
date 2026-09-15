// Kiểu dữ liệu trả về từ REST API (camelCase theo System.Text.Json)

export interface ProductCard {
  id: number; name: string; slug: string; imageUrl?: string | null; categoryName?: string | null;
  originalPrice: number; salePrice: number; discountPercent: number; hasDiscount: boolean;
  isNew: boolean; inStock: boolean; soldCount: number;
}

export interface Paged<T> { items: T[]; pageIndex: number; pageSize?: number; totalCount: number; totalPages: number; }

export interface MenuChild { id: number; name: string; slug: string; }
export interface MenuCategory extends MenuChild { imageUrl?: string | null; children: MenuChild[]; }

export interface Banner { id: number; title?: string | null; imageUrl: string; linkUrl?: string | null; }
export interface PostSummary { id: number; title: string; slug: string; summary?: string | null; thumbnailUrl?: string | null; publishedAt?: string | null; type?: number; }
export interface PostDetail extends PostSummary { content: string; author?: string | null; recent: { title: string; slug: string }[]; }

export interface HomeData {
  banners: Banner[]; categories: MenuCategory[];
  newProducts: ProductCard[]; saleProducts: ProductCard[]; bestSellers: ProductCard[];
  posts: PostSummary[]; freeShippingThreshold: number;
}

export interface Color { id: number; name: string; hexCode?: string | null; }
export interface Size { id: number; name: string; }
export interface Filters { colors: Color[]; sizes: Size[]; materials: string[]; }

export interface ProductFilter {
  q?: string; colorIds: number[]; sizeIds: number[]; minPrice?: number | null; maxPrice?: number | null;
  material?: string | null; sort: string; page: number;
}

export interface CategoryInfo { id: number; name: string; slug: string; description?: string | null; parent?: { name: string; slug: string } | null; }
export interface ProductListResponse { category?: CategoryInfo; subCategories?: MenuChild[]; products: Paged<ProductCard>; }

export interface VariantOption { id: number; colorId: number; sizeId: number; sku: string; originalPrice: number; salePrice: number; stock: number; }
export interface ProductImage { id: number; url: string; colorId?: number | null; isMain: boolean; }
export interface Review { id: number; userName: string; rating: number; comment?: string | null; createdAt: string; }
export interface ProductDetail {
  id: number; code: string; name: string; slug: string; material?: string | null; shortDescription?: string | null; description?: string | null;
  soldCount: number; isNew: boolean;
  category: { name: string; slug: string; parent?: { name: string; slug: string } | null };
  price: { originalPrice: number; salePrice: number; discountPercent: number; hasDiscount: boolean };
  images: ProductImage[]; colors: Color[]; sizes: Size[]; variants: VariantOption[];
  reviews: Review[]; averageRating: number; related: ProductCard[]; inWishlist: boolean;
}

export interface CartItemLocal { variantId: number; quantity: number; }
export interface CartLine {
  variantId: number; productId: number; productName: string; slug: string; imageUrl?: string | null;
  colorName: string; sizeName: string; sku: string; originalPrice: number; unitPrice: number;
  quantity: number; stock: number; isActive: boolean; lineTotal: number; isAvailable: boolean;
}
export interface CartSummary {
  lines: CartLine[]; subTotal: number; totalQuantity: number; couponCode?: string | null;
  coupon?: { code: string; description?: string | null } | null; discountAmount: number; couponError?: string | null;
  shippingFee: number; freeShippingThreshold: number; total: number; isEmpty: boolean;
}

export interface UserInfo { id: string; fullName: string; email?: string | null; phone?: string | null; roles: string[]; }
export interface AuthResponse { token: string; expiresAt: string; user: UserInfo; }

export interface CheckoutPayload {
  customerName: string; phone: string; email?: string | null; province: string; district: string; ward: string; street: string;
  note?: string | null; paymentMethod: number; saveAddress: boolean; items?: CartItemLocal[]; couponCode?: string | null;
}
export interface CheckoutResult { orderCode: string; totalAmount: number; paymentMethod: number; bankAccount?: string | null; }

export interface OrderLine { id: number; variantId: number; productName: string; sku: string; colorName: string; sizeName: string; unitPrice: number; quantity: number; lineTotal: number; }
export interface OrderHistory { status: number; statusName: string; note?: string | null; changedAt: string; }
export interface OrderDetail {
  id: number; orderCode: string; customerName: string; phone: string; email?: string | null;
  shippingAddress: string; province: string; district: string; ward: string; note?: string | null;
  subTotal: number; shippingFee: number; discountAmount: number; totalAmount: number;
  paymentMethod: number; paymentMethodName: string; paymentStatus: number; paymentStatusName: string;
  status: number; statusName: string; cancelReason?: string | null; createdAt: string; completedAt?: string | null; canCancel: boolean;
  details: OrderLine[]; histories: OrderHistory[];
}
export interface OrderSummary { id: number; orderCode: string; createdAt: string; totalAmount: number; status: number; statusName: string; itemCount: number; }
export interface OrderDetailResponse { order: OrderDetail; products: { variantId: number; productId: number; slug: string; reviewed: boolean }[]; }

export interface Address { id: number; receiverName: string; phone: string; province: string; district: string; ward: string; street: string; isDefault: boolean; }
export interface Store { id: number; name: string; address: string; phone?: string | null; openingHours?: string | null; mapEmbedUrl?: string | null; }
export interface Settings { storeName?: string; hotline?: string; email?: string; freeShippingThreshold: number; defaultShippingFee: number; bankAccount?: string | null; }
export interface Profile { fullName: string; email?: string | null; phoneNumber?: string | null; gender?: number | null; dateOfBirth?: string | null; createdAt: string; }

/** Lớp CSS badge theo trạng thái đơn (khớp EnumExtensions.ToBadgeClass ở backend). */
export function statusBadge(status: number): string {
  switch (status) {
    case 0: return 'bg-warning text-dark';
    case 1: return 'bg-info text-dark';
    case 2: return 'bg-primary';
    case 3: return 'bg-success';
    case 4: return 'bg-secondary';
    default: return 'bg-light text-dark';
  }
}
