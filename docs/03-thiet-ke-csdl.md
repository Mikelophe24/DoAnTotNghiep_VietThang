# CHƯƠNG 4 (phần 1). THIẾT KẾ CƠ SỞ DỮ LIỆU

Hệ quản trị: **SQL Server 2019+**. Cách xây dựng: **EF Core Code First** (entity C# → Migration → CSDL). Script SQL tương ứng ở `04-schema.sql` dùng để đối chiếu và đưa vào phụ lục báo cáo.

Quy ước:
- Khóa chính `Id INT IDENTITY`, trừ bảng Identity dùng `NVARCHAR(450)` theo chuẩn ASP.NET Core Identity.
- Tiền tệ dùng `DECIMAL(18,0)` (VNĐ không có phần lẻ).
- Chuỗi tiếng Việt dùng `NVARCHAR`.
- Các trạng thái lưu dạng `TINYINT` ánh xạ enum C# (bảng mã ở mục 4.4).

## 4.1. Sơ đồ thực thể – liên kết (ERD)

```mermaid
erDiagram
  AspNetUsers ||--o{ AspNetUserRoles : "có vai trò"
  AspNetRoles ||--o{ AspNetUserRoles : ""
  AspNetUsers ||--o{ Addresses : "sổ địa chỉ"
  AspNetUsers ||--o| Carts : "giỏ hàng"
  AspNetUsers ||--o{ Orders : "đặt"
  AspNetUsers ||--o{ Reviews : "viết"
  AspNetUsers ||--o{ Wishlists : "yêu thích"
  AspNetUsers ||--o{ GoodsReceipts : "lập phiếu"
  AspNetUsers ||--o{ Posts : "viết bài"

  Categories ||--o{ Categories : "danh mục con"
  Categories ||--o{ Products : "chứa"
  Products ||--o{ ProductImages : "ảnh"
  Products ||--o{ ProductVariants : "biến thể"
  Colors ||--o{ ProductVariants : ""
  Sizes ||--o{ ProductVariants : ""
  Colors ||--o{ ProductImages : "ảnh theo màu"
  Products ||--o{ PromotionProducts : ""
  Promotions ||--o{ PromotionProducts : "áp dụng"
  Products ||--o{ Reviews : "được đánh giá"
  Products ||--o{ Wishlists : ""

  Carts ||--o{ CartItems : "gồm"
  ProductVariants ||--o{ CartItems : ""

  Orders ||--o{ OrderDetails : "gồm"
  ProductVariants ||--o{ OrderDetails : ""
  Coupons ||--o{ Orders : "áp mã"
  Orders ||--o{ OrderStatusHistories : "lịch sử"
  Orders ||--o{ Reviews : "đánh giá sau mua"

  Suppliers ||--o{ GoodsReceipts : "cung cấp"
  GoodsReceipts ||--o{ GoodsReceiptDetails : "gồm"
  ProductVariants ||--o{ GoodsReceiptDetails : ""
  ProductVariants ||--o{ InventoryTransactions : "nhật ký kho"

  AspNetUsers {
    nvarchar450 Id PK
    nvarchar256 UserName
    nvarchar256 Email
    nvarchar PasswordHash
    nvarchar PhoneNumber
    nvarchar100 FullName
    tinyint Gender
    date DateOfBirth
    nvarchar AvatarUrl
    bit IsActive
    datetime2 CreatedAt
  }
  Addresses {
    int Id PK
    nvarchar450 UserId FK
    nvarchar100 ReceiverName
    nvarchar20 Phone
    nvarchar100 Province
    nvarchar100 District
    nvarchar100 Ward
    nvarchar255 Street
    bit IsDefault
  }
  Categories {
    int Id PK
    nvarchar100 Name
    nvarchar120 Slug UK
    int ParentId FK
    nvarchar500 Description
    nvarchar ImageUrl
    int DisplayOrder
    bit IsActive
  }
  Products {
    int Id PK
    nvarchar50 Code UK
    nvarchar200 Name
    nvarchar220 Slug UK
    int CategoryId FK
    nvarchar100 Material
    nvarchar500 ShortDescription
    nvarchar Description
    decimal Price
    bit IsNew
    bit IsFeatured
    bit IsActive
    int ViewCount
    int SoldCount
    datetime2 CreatedAt
    datetime2 UpdatedAt
  }
  ProductImages {
    int Id PK
    int ProductId FK
    int ColorId FK
    nvarchar Url
    bit IsMain
    int DisplayOrder
  }
  Colors {
    int Id PK
    nvarchar50 Name
    char7 HexCode
  }
  Sizes {
    int Id PK
    nvarchar20 Name
    int DisplayOrder
  }
  ProductVariants {
    int Id PK
    int ProductId FK
    int ColorId FK
    int SizeId FK
    nvarchar60 Sku UK
    decimal Price
    decimal CostPrice
    int StockQuantity
    bit IsActive
  }
  Promotions {
    int Id PK
    nvarchar150 Name
    tinyint DiscountType
    decimal DiscountValue
    datetime2 StartDate
    datetime2 EndDate
    bit IsActive
  }
  PromotionProducts {
    int PromotionId PK_FK
    int ProductId PK_FK
  }
  Coupons {
    int Id PK
    nvarchar30 Code UK
    nvarchar200 Description
    tinyint DiscountType
    decimal DiscountValue
    decimal MaxDiscountAmount
    decimal MinOrderAmount
    int UsageLimit
    int UsedCount
    datetime2 StartDate
    datetime2 EndDate
    bit IsActive
  }
  Carts {
    int Id PK
    nvarchar450 UserId FK_UK
    datetime2 UpdatedAt
  }
  CartItems {
    int Id PK
    int CartId FK
    int VariantId FK
    int Quantity
  }
  Orders {
    int Id PK
    nvarchar20 OrderCode UK
    nvarchar450 UserId FK
    nvarchar100 CustomerName
    nvarchar20 Phone
    nvarchar100 Email
    nvarchar255 ShippingAddress
    nvarchar100 Province
    nvarchar100 District
    nvarchar100 Ward
    nvarchar500 Note
    decimal SubTotal
    decimal ShippingFee
    decimal DiscountAmount
    int CouponId FK
    decimal TotalAmount
    tinyint PaymentMethod
    tinyint PaymentStatus
    tinyint Status
    nvarchar255 CancelReason
    datetime2 CreatedAt
    datetime2 CompletedAt
  }
  OrderDetails {
    int Id PK
    int OrderId FK
    int VariantId FK
    nvarchar200 ProductName
    nvarchar60 Sku
    nvarchar50 ColorName
    nvarchar20 SizeName
    decimal UnitPrice
    int Quantity
  }
  OrderStatusHistories {
    int Id PK
    int OrderId FK
    tinyint FromStatus
    tinyint ToStatus
    nvarchar255 Note
    nvarchar450 ChangedByUserId FK
    datetime2 ChangedAt
  }
  Suppliers {
    int Id PK
    nvarchar150 Name
    nvarchar100 ContactName
    nvarchar20 Phone
    nvarchar100 Email
    nvarchar255 Address
    bit IsActive
  }
  GoodsReceipts {
    int Id PK
    nvarchar20 Code UK
    int SupplierId FK
    nvarchar450 CreatedByUserId FK
    datetime2 ReceiptDate
    tinyint Status
    decimal TotalAmount
    nvarchar500 Note
    datetime2 CreatedAt
    datetime2 CompletedAt
  }
  GoodsReceiptDetails {
    int Id PK
    int GoodsReceiptId FK
    int VariantId FK
    int Quantity
    decimal UnitCost
  }
  InventoryTransactions {
    int Id PK
    int VariantId FK
    tinyint Type
    int Quantity
    int StockAfter
    nvarchar30 ReferenceType
    int ReferenceId
    nvarchar255 Note
    nvarchar450 CreatedByUserId FK
    datetime2 CreatedAt
  }
  Reviews {
    int Id PK
    int ProductId FK
    nvarchar450 UserId FK
    int OrderId FK
    tinyint Rating
    nvarchar1000 Comment
    bit IsApproved
    datetime2 CreatedAt
  }
  Wishlists {
    nvarchar450 UserId PK_FK
    int ProductId PK_FK
    datetime2 CreatedAt
  }
  Posts {
    int Id PK
    nvarchar200 Title
    nvarchar220 Slug UK
    nvarchar500 Summary
    nvarchar Content
    nvarchar ThumbnailUrl
    tinyint Type
    nvarchar450 AuthorId FK
    bit IsPublished
    datetime2 PublishedAt
    datetime2 CreatedAt
  }
  Banners {
    int Id PK
    nvarchar150 Title
    nvarchar ImageUrl
    nvarchar LinkUrl
    tinyint Position
    int DisplayOrder
    bit IsActive
  }
  Stores {
    int Id PK
    nvarchar150 Name
    nvarchar255 Address
    nvarchar20 Phone
    nvarchar100 OpeningHours
    nvarchar MapEmbedUrl
    bit IsActive
  }
  Contacts {
    int Id PK
    nvarchar100 FullName
    nvarchar100 Email
    nvarchar20 Phone
    nvarchar200 Subject
    nvarchar2000 Message
    bit IsHandled
    datetime2 CreatedAt
  }
  Settings {
    nvarchar50 Key PK
    nvarchar Value
    nvarchar200 Description
  }
```

## 4.2. Nhóm bảng

| Nhóm | Bảng | Vai trò |
|---|---|---|
| Người dùng | AspNetUsers, AspNetRoles, AspNetUserRoles (+ 4 bảng Identity phụ), Addresses | Tài khoản, vai trò Admin/Employee/Customer, sổ địa chỉ |
| Danh mục sản phẩm | Categories, Products, ProductImages, Colors, Sizes, ProductVariants | Sản phẩm và biến thể màu × size, tồn kho trên biến thể |
| Khuyến mãi | Promotions, PromotionProducts, Coupons | Giảm giá theo sản phẩm; mã giảm giá khi thanh toán |
| Bán hàng | Carts, CartItems, Orders, OrderDetails, OrderStatusHistories | Giỏ hàng, đơn hàng, lịch sử trạng thái |
| Kho | Suppliers, GoodsReceipts, GoodsReceiptDetails, InventoryTransactions | Nhà cung cấp, phiếu nhập, nhật ký xuất/nhập/điều chỉnh |
| Tương tác | Reviews, Wishlists, Contacts | Đánh giá, yêu thích, liên hệ |
| Nội dung | Posts, Banners, Stores, Settings | Tin tức/tuyển dụng, banner, hệ thống cửa hàng, cấu hình |

## 4.3. Mô tả chi tiết các bảng nghiệp vụ chính

### Products – Sản phẩm
| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK, IDENTITY | |
| Code | NVARCHAR(50) | UNIQUE | Mã sản phẩm (VD: VT-BL-001) |
| Name | NVARCHAR(200) | NOT NULL | Tên (VD: Bộ lanh nữ quần lửng họa tiết hoa) |
| Slug | NVARCHAR(220) | UNIQUE | URL thân thiện |
| CategoryId | INT | FK → Categories | Danh mục cấp cuối |
| Material | NVARCHAR(100) | | Chất liệu: Lanh, Cotton, Thun lạnh… |
| ShortDescription | NVARCHAR(500) | | Mô tả ngắn |
| Description | NVARCHAR(MAX) | | Mô tả chi tiết (HTML) |
| Price | DECIMAL(18,0) | NOT NULL, ≥ 0 | Giá niêm yết mặc định cho mọi biến thể |
| IsNew / IsFeatured / IsActive | BIT | DEFAULT | Hàng mới về / Nổi bật / Đang bán |
| ViewCount / SoldCount | INT | DEFAULT 0 | Thống kê |
| CreatedAt / UpdatedAt | DATETIME2 | | |

### ProductVariants – Biến thể (màu × size)
| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK | |
| ProductId | INT | FK → Products | |
| ColorId | INT | FK → Colors | |
| SizeId | INT | FK → Sizes | |
| Sku | NVARCHAR(60) | UNIQUE | VD: VT-BL-001-XANH-M |
| Price | DECIMAL(18,0) | NULL | Giá riêng; NULL thì dùng Products.Price |
| CostPrice | DECIMAL(18,0) | DEFAULT 0 | Giá vốn gần nhất (cập nhật khi nhập kho) |
| StockQuantity | INT | CHECK ≥ 0 | Tồn kho hiện tại |
| IsActive | BIT | | |
| *Unique* | (ProductId, ColorId, SizeId) | | Một tổ hợp màu-size chỉ có 1 biến thể |

### Orders – Đơn hàng
| Cột | Kiểu | Ràng buộc | Mô tả |
|---|---|---|---|
| Id | INT | PK | |
| OrderCode | NVARCHAR(20) | UNIQUE | VD: VT240914001 (dùng để tra cứu) |
| UserId | NVARCHAR(450) | FK → AspNetUsers, NULL | NULL nếu khách vãng lai |
| CustomerName, Phone, Email | NVARCHAR | NOT NULL (Email NULL) | Thông tin người nhận (snapshot) |
| ShippingAddress, Province, District, Ward | NVARCHAR | | Địa chỉ giao |
| Note | NVARCHAR(500) | | Ghi chú khách |
| SubTotal | DECIMAL(18,0) | | Tổng tiền hàng |
| ShippingFee | DECIMAL(18,0) | | Phí ship (0 nếu ≥ ngưỡng freeship) |
| DiscountAmount | DECIMAL(18,0) | | Tiền giảm từ coupon |
| CouponId | INT | FK → Coupons, NULL | |
| TotalAmount | DECIMAL(18,0) | | = SubTotal + ShippingFee − DiscountAmount |
| PaymentMethod | TINYINT | | 1 COD, 2 Chuyển khoản |
| PaymentStatus | TINYINT | | 0 Chưa thanh toán, 1 Đã thanh toán, 2 Hoàn tiền |
| Status | TINYINT | | 0 Chờ xác nhận, 1 Đã xác nhận, 2 Đang giao, 3 Hoàn thành, 4 Đã hủy |
| CancelReason | NVARCHAR(255) | | |
| CreatedAt, CompletedAt | DATETIME2 | | |

### OrderDetails – Chi tiết đơn
Lưu **snapshot** tên sản phẩm, SKU, màu, size, đơn giá tại thời điểm mua để báo cáo không bị sai khi sản phẩm đổi tên/giá sau này.

| Cột | Kiểu | Mô tả |
|---|---|---|
| Id | INT PK | |
| OrderId | INT FK → Orders (CASCADE) | |
| VariantId | INT FK → ProductVariants | Để hoàn tồn khi hủy và thống kê |
| ProductName, Sku, ColorName, SizeName | NVARCHAR | Snapshot |
| UnitPrice | DECIMAL(18,0) | Giá sau khuyến mãi sản phẩm tại thời điểm mua |
| Quantity | INT CHECK > 0 | |

### GoodsReceipts – Phiếu nhập hàng
| Cột | Kiểu | Mô tả |
|---|---|---|
| Id | INT PK | |
| Code | NVARCHAR(20) UNIQUE | VD: PN240914001 |
| SupplierId | INT FK → Suppliers | |
| CreatedByUserId | NVARCHAR(450) FK → AspNetUsers | Nhân viên lập |
| ReceiptDate | DATETIME2 | Ngày nhập |
| Status | TINYINT | 0 Nháp, 1 Đã nhập kho, 2 Đã hủy |
| TotalAmount | DECIMAL(18,0) | Σ Quantity × UnitCost |
| Note | NVARCHAR(500) | |
| CreatedAt, CompletedAt | DATETIME2 | |

### InventoryTransactions – Nhật ký kho
Mỗi biến động tồn kho ghi một dòng, giúp truy vết và đối soát.

| Cột | Kiểu | Mô tả |
|---|---|---|
| Id | INT PK | |
| VariantId | INT FK → ProductVariants | |
| Type | TINYINT | 1 Nhập kho, 2 Xuất bán, 3 Hoàn (hủy đơn), 4 Điều chỉnh kiểm kê |
| Quantity | INT | Dương = tăng, âm = giảm |
| StockAfter | INT | Tồn sau giao dịch |
| ReferenceType / ReferenceId | NVARCHAR(30) / INT | "GoodsReceipt"/Id, "Order"/Id, "Adjustment" |
| Note | NVARCHAR(255) | |
| CreatedByUserId | NVARCHAR(450) FK | NULL nếu do khách đặt hàng |
| CreatedAt | DATETIME2 | |

### Coupons – Mã giảm giá
| Cột | Kiểu | Mô tả |
|---|---|---|
| Code | NVARCHAR(30) UNIQUE | VD: VT10, FREESHIP |
| DiscountType | TINYINT | 1 Phần trăm, 2 Số tiền |
| DiscountValue | DECIMAL(18,0) | 10 (%) hoặc 30000 (đ) |
| MaxDiscountAmount | DECIMAL(18,0) NULL | Giảm tối đa (với loại %) |
| MinOrderAmount | DECIMAL(18,0) | Đơn tối thiểu |
| UsageLimit / UsedCount | INT | Giới hạn / đã dùng |
| StartDate / EndDate | DATETIME2 | Hiệu lực |
| IsActive | BIT | |

## 4.4. Bảng mã enum

| Enum | Giá trị |
|---|---|
| OrderStatus | 0 Pending (Chờ xác nhận), 1 Confirmed (Đã xác nhận), 2 Shipping (Đang giao), 3 Completed (Hoàn thành), 4 Cancelled (Đã hủy) |
| PaymentMethod | 1 COD, 2 BankTransfer |
| PaymentStatus | 0 Unpaid, 1 Paid, 2 Refunded |
| DiscountType | 1 Percent, 2 Amount |
| ReceiptStatus | 0 Draft, 1 Completed, 2 Cancelled |
| InventoryType | 1 Import, 2 Export, 3 Return, 4 Adjust |
| PostType | 1 News (Tin tức), 2 Recruitment (Tuyển dụng) |
| BannerPosition | 1 HomeSlider, 2 HomeMiddle, 3 CategoryTop |
| Gender | 0 Khác, 1 Nam, 2 Nữ |

Chuyển trạng thái đơn hợp lệ: Pending → Confirmed | Cancelled; Confirmed → Shipping | Cancelled; Shipping → Completed; Completed/Cancelled là trạng thái cuối.

## 4.5. Quy tắc tính giá

1. **Giá gốc biến thể** = `Variant.Price ?? Product.Price`.
2. **Giá khuyến mãi** = áp chương trình Promotion còn hiệu lực có mức giảm lớn nhất trên sản phẩm (Percent: giá × (1 − v/100); Amount: giá − v; không âm).
3. **SubTotal** = Σ (Giá khuyến mãi × số lượng).
4. **DiscountAmount** (coupon) = Percent: min(SubTotal × v/100, MaxDiscountAmount); Amount: v. Chỉ áp khi SubTotal ≥ MinOrderAmount, còn hạn, UsedCount < UsageLimit.
5. **ShippingFee** = 0 nếu SubTotal ≥ Settings["FreeShippingThreshold"] (mặc định 500.000đ), ngược lại Settings["DefaultShippingFee"] (mặc định 30.000đ).
6. **TotalAmount** = SubTotal + ShippingFee − DiscountAmount.

## 4.6. Chỉ mục (Index) đề xuất
- Products(CategoryId, IsActive), Products(Slug), Products(Name) – lọc và tìm kiếm.
- ProductVariants(ProductId), ProductVariants(Sku).
- Orders(Status, CreatedAt), Orders(UserId), Orders(OrderCode), Orders(Phone) – quản lý và tra cứu đơn.
- OrderDetails(VariantId) – thống kê bán chạy.
- InventoryTransactions(VariantId, CreatedAt).
- Reviews(ProductId, IsApproved).
