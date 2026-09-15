/* =====================================================================
   CSDL: VietThangFashion  -  Website quản lý cửa hàng thời trang Việt Thắng
   SQL Server 2019+
   Ghi chú: Trong dự án thực tế CSDL được tạo bằng EF Core Migration.
            Script này dùng để đối chiếu thiết kế và đưa vào phụ lục báo cáo.
   ===================================================================== */

IF DB_ID('VietThangFashion') IS NULL
    CREATE DATABASE VietThangFashion;
GO
USE VietThangFashion;
GO

/* ---------------------- 1. ASP.NET Core Identity ---------------------- */
CREATE TABLE AspNetRoles (
    Id               NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name             NVARCHAR(256) NULL,
    NormalizedName   NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;

CREATE TABLE AspNetUsers (
    Id                   NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName             NVARCHAR(256) NULL,
    NormalizedUserName   NVARCHAR(256) NULL,
    Email                NVARCHAR(256) NULL,
    NormalizedEmail      NVARCHAR(256) NULL,
    EmailConfirmed       BIT NOT NULL DEFAULT 0,
    PasswordHash         NVARCHAR(MAX) NULL,
    SecurityStamp        NVARCHAR(MAX) NULL,
    ConcurrencyStamp     NVARCHAR(MAX) NULL,
    PhoneNumber          NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled     BIT NOT NULL DEFAULT 0,
    LockoutEnd           DATETIMEOFFSET NULL,
    LockoutEnabled       BIT NOT NULL DEFAULT 1,
    AccessFailedCount    INT NOT NULL DEFAULT 0,
    -- Cột mở rộng (ApplicationUser)
    FullName             NVARCHAR(100) NOT NULL,
    Gender               TINYINT NULL,               -- 0 Khác, 1 Nam, 2 Nữ
    DateOfBirth          DATE NULL,
    AvatarUrl            NVARCHAR(500) NULL,
    IsActive             BIT NOT NULL DEFAULT 1,
    CreatedAt            DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON AspNetUsers(NormalizedEmail);

CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    RoleId NVARCHAR(450) NOT NULL REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    PRIMARY KEY (UserId, RoleId)
);
/* Các bảng Identity phụ (AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims)
   được Migration sinh tự động, không liệt kê ở đây. */

CREATE TABLE Addresses (
    Id           INT IDENTITY PRIMARY KEY,
    UserId       NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    ReceiverName NVARCHAR(100) NOT NULL,
    Phone        NVARCHAR(20)  NOT NULL,
    Province     NVARCHAR(100) NOT NULL,
    District     NVARCHAR(100) NOT NULL,
    Ward         NVARCHAR(100) NOT NULL,
    Street       NVARCHAR(255) NOT NULL,
    IsDefault    BIT NOT NULL DEFAULT 0
);

/* ---------------------- 2. Danh mục & sản phẩm ---------------------- */
CREATE TABLE Categories (
    Id           INT IDENTITY PRIMARY KEY,
    Name         NVARCHAR(100) NOT NULL,
    Slug         NVARCHAR(120) NOT NULL UNIQUE,
    ParentId     INT NULL REFERENCES Categories(Id),
    Description  NVARCHAR(500) NULL,
    ImageUrl     NVARCHAR(500) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1
);

CREATE TABLE Colors (
    Id      INT IDENTITY PRIMARY KEY,
    Name    NVARCHAR(50) NOT NULL UNIQUE,
    HexCode CHAR(7) NULL
);

CREATE TABLE Sizes (
    Id           INT IDENTITY PRIMARY KEY,
    Name         NVARCHAR(20) NOT NULL UNIQUE,
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE Products (
    Id               INT IDENTITY PRIMARY KEY,
    Code             NVARCHAR(50)  NOT NULL UNIQUE,
    Name             NVARCHAR(200) NOT NULL,
    Slug             NVARCHAR(220) NOT NULL UNIQUE,
    CategoryId       INT NOT NULL REFERENCES Categories(Id),
    Material         NVARCHAR(100) NULL,
    ShortDescription NVARCHAR(500) NULL,
    Description      NVARCHAR(MAX) NULL,
    Price            DECIMAL(18,0) NOT NULL CHECK (Price >= 0),
    IsNew            BIT NOT NULL DEFAULT 0,
    IsFeatured       BIT NOT NULL DEFAULT 0,
    IsActive         BIT NOT NULL DEFAULT 1,
    ViewCount        INT NOT NULL DEFAULT 0,
    SoldCount        INT NOT NULL DEFAULT 0,
    CreatedAt        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt        DATETIME2 NULL
);
CREATE INDEX IX_Products_Category_Active ON Products(CategoryId, IsActive);
CREATE INDEX IX_Products_Name ON Products(Name);

CREATE TABLE ProductImages (
    Id           INT IDENTITY PRIMARY KEY,
    ProductId    INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    ColorId      INT NULL REFERENCES Colors(Id),
    Url          NVARCHAR(500) NOT NULL,
    IsMain       BIT NOT NULL DEFAULT 0,
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE ProductVariants (
    Id            INT IDENTITY PRIMARY KEY,
    ProductId     INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    ColorId       INT NOT NULL REFERENCES Colors(Id),
    SizeId        INT NOT NULL REFERENCES Sizes(Id),
    Sku           NVARCHAR(60) NOT NULL UNIQUE,
    Price         DECIMAL(18,0) NULL,                 -- NULL => dùng Products.Price
    CostPrice     DECIMAL(18,0) NOT NULL DEFAULT 0,   -- giá vốn gần nhất
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    IsActive      BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Variant UNIQUE (ProductId, ColorId, SizeId)
);

/* ---------------------- 3. Khuyến mãi ---------------------- */
CREATE TABLE Promotions (
    Id            INT IDENTITY PRIMARY KEY,
    Name          NVARCHAR(150) NOT NULL,
    DiscountType  TINYINT NOT NULL,                   -- 1 Percent, 2 Amount
    DiscountValue DECIMAL(18,0) NOT NULL CHECK (DiscountValue > 0),
    StartDate     DATETIME2 NOT NULL,
    EndDate       DATETIME2 NOT NULL,
    IsActive      BIT NOT NULL DEFAULT 1,
    CHECK (EndDate > StartDate)
);

CREATE TABLE PromotionProducts (
    PromotionId INT NOT NULL REFERENCES Promotions(Id) ON DELETE CASCADE,
    ProductId   INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    PRIMARY KEY (PromotionId, ProductId)
);

CREATE TABLE Coupons (
    Id                INT IDENTITY PRIMARY KEY,
    Code              NVARCHAR(30) NOT NULL UNIQUE,
    Description       NVARCHAR(200) NULL,
    DiscountType      TINYINT NOT NULL,               -- 1 Percent, 2 Amount
    DiscountValue     DECIMAL(18,0) NOT NULL,
    MaxDiscountAmount DECIMAL(18,0) NULL,
    MinOrderAmount    DECIMAL(18,0) NOT NULL DEFAULT 0,
    UsageLimit        INT NULL,
    UsedCount         INT NOT NULL DEFAULT 0,
    StartDate         DATETIME2 NOT NULL,
    EndDate           DATETIME2 NOT NULL,
    IsActive          BIT NOT NULL DEFAULT 1
);

/* ---------------------- 4. Giỏ hàng & đơn hàng ---------------------- */
CREATE TABLE Carts (
    Id        INT IDENTITY PRIMARY KEY,
    UserId    NVARCHAR(450) NOT NULL UNIQUE REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    UpdatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE CartItems (
    Id        INT IDENTITY PRIMARY KEY,
    CartId    INT NOT NULL REFERENCES Carts(Id) ON DELETE CASCADE,
    VariantId INT NOT NULL REFERENCES ProductVariants(Id),
    Quantity  INT NOT NULL CHECK (Quantity > 0),
    CONSTRAINT UQ_CartItem UNIQUE (CartId, VariantId)
);

CREATE TABLE Orders (
    Id              INT IDENTITY PRIMARY KEY,
    OrderCode       NVARCHAR(20) NOT NULL UNIQUE,
    UserId          NVARCHAR(450) NULL REFERENCES AspNetUsers(Id),
    CustomerName    NVARCHAR(100) NOT NULL,
    Phone           NVARCHAR(20)  NOT NULL,
    Email           NVARCHAR(100) NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    Province        NVARCHAR(100) NOT NULL,
    District        NVARCHAR(100) NOT NULL,
    Ward            NVARCHAR(100) NOT NULL,
    Note            NVARCHAR(500) NULL,
    SubTotal        DECIMAL(18,0) NOT NULL,
    ShippingFee     DECIMAL(18,0) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,0) NOT NULL DEFAULT 0,
    CouponId        INT NULL REFERENCES Coupons(Id),
    TotalAmount     DECIMAL(18,0) NOT NULL,
    PaymentMethod   TINYINT NOT NULL,                 -- 1 COD, 2 BankTransfer
    PaymentStatus   TINYINT NOT NULL DEFAULT 0,       -- 0 Unpaid, 1 Paid, 2 Refunded
    Status          TINYINT NOT NULL DEFAULT 0,       -- 0 Pending,1 Confirmed,2 Shipping,3 Completed,4 Cancelled
    CancelReason    NVARCHAR(255) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CompletedAt     DATETIME2 NULL
);
CREATE INDEX IX_Orders_Status_Created ON Orders(Status, CreatedAt);
CREATE INDEX IX_Orders_User ON Orders(UserId);
CREATE INDEX IX_Orders_Phone ON Orders(Phone);

CREATE TABLE OrderDetails (
    Id          INT IDENTITY PRIMARY KEY,
    OrderId     INT NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    VariantId   INT NOT NULL REFERENCES ProductVariants(Id),
    ProductName NVARCHAR(200) NOT NULL,
    Sku         NVARCHAR(60)  NOT NULL,
    ColorName   NVARCHAR(50)  NOT NULL,
    SizeName    NVARCHAR(20)  NOT NULL,
    UnitPrice   DECIMAL(18,0) NOT NULL,
    Quantity    INT NOT NULL CHECK (Quantity > 0),
    LineTotal   AS (UnitPrice * Quantity) PERSISTED
);
CREATE INDEX IX_OrderDetails_Variant ON OrderDetails(VariantId);

CREATE TABLE OrderStatusHistories (
    Id              INT IDENTITY PRIMARY KEY,
    OrderId         INT NOT NULL REFERENCES Orders(Id) ON DELETE CASCADE,
    FromStatus      TINYINT NULL,
    ToStatus        TINYINT NOT NULL,
    Note            NVARCHAR(255) NULL,
    ChangedByUserId NVARCHAR(450) NULL REFERENCES AspNetUsers(Id),
    ChangedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

/* ---------------------- 5. Kho & nhập hàng ---------------------- */
CREATE TABLE Suppliers (
    Id          INT IDENTITY PRIMARY KEY,
    Name        NVARCHAR(150) NOT NULL,
    ContactName NVARCHAR(100) NULL,
    Phone       NVARCHAR(20)  NULL,
    Email       NVARCHAR(100) NULL,
    Address     NVARCHAR(255) NULL,
    IsActive    BIT NOT NULL DEFAULT 1
);

CREATE TABLE GoodsReceipts (
    Id              INT IDENTITY PRIMARY KEY,
    Code            NVARCHAR(20) NOT NULL UNIQUE,
    SupplierId      INT NOT NULL REFERENCES Suppliers(Id),
    CreatedByUserId NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id),
    ReceiptDate     DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Status          TINYINT NOT NULL DEFAULT 0,       -- 0 Draft, 1 Completed, 2 Cancelled
    TotalAmount     DECIMAL(18,0) NOT NULL DEFAULT 0,
    Note            NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CompletedAt     DATETIME2 NULL
);

CREATE TABLE GoodsReceiptDetails (
    Id             INT IDENTITY PRIMARY KEY,
    GoodsReceiptId INT NOT NULL REFERENCES GoodsReceipts(Id) ON DELETE CASCADE,
    VariantId      INT NOT NULL REFERENCES ProductVariants(Id),
    Quantity       INT NOT NULL CHECK (Quantity > 0),
    UnitCost       DECIMAL(18,0) NOT NULL CHECK (UnitCost >= 0)
);

CREATE TABLE InventoryTransactions (
    Id              INT IDENTITY PRIMARY KEY,
    VariantId       INT NOT NULL REFERENCES ProductVariants(Id),
    Type            TINYINT NOT NULL,                 -- 1 Import, 2 Export, 3 Return, 4 Adjust
    Quantity        INT NOT NULL,                     -- (+) tăng, (-) giảm
    StockAfter      INT NOT NULL,
    ReferenceType   NVARCHAR(30) NULL,                -- 'GoodsReceipt' | 'Order' | 'Adjustment'
    ReferenceId     INT NULL,
    Note            NVARCHAR(255) NULL,
    CreatedByUserId NVARCHAR(450) NULL REFERENCES AspNetUsers(Id),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Inventory_Variant_Date ON InventoryTransactions(VariantId, CreatedAt);

/* ---------------------- 6. Tương tác ---------------------- */
CREATE TABLE Reviews (
    Id         INT IDENTITY PRIMARY KEY,
    ProductId  INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    UserId     NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id),
    OrderId    INT NULL REFERENCES Orders(Id),
    Rating     TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment    NVARCHAR(1000) NULL,
    IsApproved BIT NOT NULL DEFAULT 0,
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Reviews_Product_Approved ON Reviews(ProductId, IsApproved);

CREATE TABLE Wishlists (
    UserId    NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    ProductId INT NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    PRIMARY KEY (UserId, ProductId)
);

CREATE TABLE Contacts (
    Id              INT IDENTITY PRIMARY KEY,
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(100) NULL,
    Phone           NVARCHAR(20)  NULL,
    Subject         NVARCHAR(200) NULL,
    Message         NVARCHAR(2000) NOT NULL,
    IsHandled       BIT NOT NULL DEFAULT 0,
    HandledByUserId NVARCHAR(450) NULL REFERENCES AspNetUsers(Id),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

/* ---------------------- 7. Nội dung & cấu hình ---------------------- */
CREATE TABLE Posts (
    Id           INT IDENTITY PRIMARY KEY,
    Title        NVARCHAR(200) NOT NULL,
    Slug         NVARCHAR(220) NOT NULL UNIQUE,
    Summary      NVARCHAR(500) NULL,
    Content      NVARCHAR(MAX) NOT NULL,
    ThumbnailUrl NVARCHAR(500) NULL,
    Type         TINYINT NOT NULL DEFAULT 1,          -- 1 News, 2 Recruitment
    AuthorId     NVARCHAR(450) NULL REFERENCES AspNetUsers(Id),
    IsPublished  BIT NOT NULL DEFAULT 0,
    PublishedAt  DATETIME2 NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

CREATE TABLE Banners (
    Id           INT IDENTITY PRIMARY KEY,
    Title        NVARCHAR(150) NULL,
    ImageUrl     NVARCHAR(500) NOT NULL,
    LinkUrl      NVARCHAR(500) NULL,
    Position     TINYINT NOT NULL DEFAULT 1,          -- 1 HomeSlider, 2 HomeMiddle, 3 CategoryTop
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive     BIT NOT NULL DEFAULT 1
);

CREATE TABLE Stores (
    Id           INT IDENTITY PRIMARY KEY,
    Name         NVARCHAR(150) NOT NULL,
    Address      NVARCHAR(255) NOT NULL,
    Phone        NVARCHAR(20)  NULL,
    OpeningHours NVARCHAR(100) NULL,
    MapEmbedUrl  NVARCHAR(1000) NULL,
    IsActive     BIT NOT NULL DEFAULT 1
);

CREATE TABLE Settings (
    [Key]       NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Value]     NVARCHAR(MAX) NULL,
    Description NVARCHAR(200) NULL
);
GO

/* ====================== DỮ LIỆU KHỞI TẠO ====================== */
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
 (NEWID(), N'Admin',    N'ADMIN',    NEWID()),
 (NEWID(), N'Employee', N'EMPLOYEE', NEWID()),
 (NEWID(), N'Customer', N'CUSTOMER', NEWID());

INSERT INTO Colors (Name, HexCode) VALUES
 (N'Trắng','#FFFFFF'),(N'Đen','#000000'),(N'Xanh than','#1F3A5F'),(N'Xanh mint','#98D8C8'),
 (N'Hồng','#F4A7B9'),(N'Be','#D9C3A5'),(N'Đỏ đô','#7B1E3A'),(N'Vàng','#F2C14E'),(N'Tím','#8E6C9E'),(N'Xám','#9AA0A6');

INSERT INTO Sizes (Name, DisplayOrder) VALUES
 ('S',1),('M',2),('L',3),('XL',4),('XXL',5),('3XL',6),
 (N'Size 1 (1-2 tuổi)',11),(N'Size 2 (3-4 tuổi)',12),(N'Size 3 (5-6 tuổi)',13),(N'Size 4 (7-8 tuổi)',14),(N'Size 5 (9-10 tuổi)',15);

-- Danh mục cấp 1
INSERT INTO Categories (Name, Slug, ParentId, DisplayOrder) VALUES
 (N'Nữ','nu',NULL,1),(N'Trung niên','trung-nien',NULL,2),(N'Trẻ em','tre-em',NULL,3),(N'Nam','nam',NULL,4);
-- Danh mục cấp 2
INSERT INTO Categories (Name, Slug, ParentId, DisplayOrder) VALUES
 (N'Bộ đồ quần đùi nữ','bo-do-quan-dui-nu',1,1),
 (N'Bộ đồ quần lửng nữ','bo-do-quan-lung-nu',1,2),
 (N'Bộ đồ quần dài nữ','bo-do-quan-dai-nu',1,3),
 (N'Đồ lẻ nữ','do-le-nu',1,4),
 (N'Đồ teen','do-teen',1,5),
 (N'Bộ quần lửng trung niên','bo-quan-lung-trung-nien',2,1),
 (N'Bộ quần dài trung niên','bo-quan-dai-trung-nien',2,2),
 (N'Áo trung niên','ao-trung-nien',2,3),
 (N'Chân váy trung niên','chan-vay-trung-nien',2,4),
 (N'Bé trai','be-trai',3,1),
 (N'Bé gái','be-gai',3,2),
 (N'Bộ đồ nam','bo-do-nam',4,1),
 (N'Đồ lẻ nam','do-le-nam',4,2);

INSERT INTO Settings ([Key],[Value],Description) VALUES
 ('StoreName',N'Thời trang gia đình VT - Việt Thắng',N'Tên cửa hàng'),
 ('Hotline','1800646859',N'Hotline'),
 ('Email','info@thoitrangvietthang.vn',N'Email liên hệ'),
 ('DefaultShippingFee','30000',N'Phí vận chuyển mặc định (đ)'),
 ('FreeShippingThreshold','500000',N'Miễn phí vận chuyển cho đơn từ (đ)'),
 ('LowStockThreshold','5',N'Ngưỡng cảnh báo sắp hết hàng'),
 ('BankAccount',N'Ngân hàng Vietcombank - STK 0011002233445 - CTK Cửa hàng Việt Thắng',N'Thông tin chuyển khoản');
GO
