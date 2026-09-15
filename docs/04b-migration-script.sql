IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(500) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(500) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(500) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Gender] tinyint NULL,
    [DateOfBirth] date NULL,
    [AvatarUrl] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(500) NULL,
    [SecurityStamp] nvarchar(500) NULL,
    [ConcurrencyStamp] nvarchar(500) NULL,
    [PhoneNumber] nvarchar(500) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Banners] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(150) NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [LinkUrl] nvarchar(500) NULL,
    [Position] tinyint NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Banners] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Slug] nvarchar(120) NOT NULL,
    [ParentId] int NULL,
    [Description] nvarchar(500) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Colors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [HexCode] nvarchar(7) NULL,
    CONSTRAINT [PK_Colors] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Coupons] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(30) NOT NULL,
    [Description] nvarchar(200) NULL,
    [DiscountType] tinyint NOT NULL,
    [DiscountValue] decimal(18,0) NOT NULL,
    [MaxDiscountAmount] decimal(18,0) NULL,
    [MinOrderAmount] decimal(18,0) NOT NULL,
    [UsageLimit] int NULL,
    [UsedCount] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Promotions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [DiscountType] tinyint NOT NULL,
    [DiscountValue] decimal(18,0) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Promotions_Date] CHECK ([EndDate] > [StartDate])
);
GO

CREATE TABLE [Settings] (
    [Key] nvarchar(50) NOT NULL,
    [Value] nvarchar(max) NULL,
    [Description] nvarchar(200) NULL,
    CONSTRAINT [PK_Settings] PRIMARY KEY ([Key])
);
GO

CREATE TABLE [Sizes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(20) NOT NULL,
    [DisplayOrder] int NOT NULL,
    CONSTRAINT [PK_Sizes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Stores] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Address] nvarchar(255) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [OpeningHours] nvarchar(100) NULL,
    [MapEmbedUrl] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Stores] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [ContactName] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(100) NULL,
    [Address] nvarchar(255) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(500) NOT NULL,
    [ClaimType] nvarchar(500) NULL,
    [ClaimValue] nvarchar(500) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Addresses] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(500) NOT NULL,
    [ReceiverName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Province] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NOT NULL,
    [Ward] nvarchar(100) NOT NULL,
    [Street] nvarchar(255) NOT NULL,
    [IsDefault] bit NOT NULL,
    CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Addresses_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(500) NOT NULL,
    [ClaimType] nvarchar(500) NULL,
    [ClaimValue] nvarchar(500) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(500) NOT NULL,
    [ProviderKey] nvarchar(500) NOT NULL,
    [ProviderDisplayName] nvarchar(500) NULL,
    [UserId] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(500) NOT NULL,
    [RoleId] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(500) NOT NULL,
    [LoginProvider] nvarchar(500) NOT NULL,
    [Name] nvarchar(500) NOT NULL,
    [Value] nvarchar(500) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Carts] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(500) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Carts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Carts_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Contacts] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [Subject] nvarchar(200) NULL,
    [Message] nvarchar(2000) NOT NULL,
    [IsHandled] bit NOT NULL,
    [HandledByUserId] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Contacts_AspNetUsers_HandledByUserId] FOREIGN KEY ([HandledByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [Posts] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(200) NOT NULL,
    [Slug] nvarchar(220) NOT NULL,
    [Summary] nvarchar(500) NULL,
    [Content] nvarchar(max) NOT NULL,
    [ThumbnailUrl] nvarchar(500) NULL,
    [Type] tinyint NOT NULL,
    [AuthorId] nvarchar(500) NULL,
    [IsPublished] bit NOT NULL,
    [PublishedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Posts_AspNetUsers_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Slug] nvarchar(220) NOT NULL,
    [CategoryId] int NOT NULL,
    [Material] nvarchar(100) NULL,
    [ShortDescription] nvarchar(500) NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(18,0) NOT NULL,
    [IsNew] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [ViewCount] int NOT NULL,
    [SoldCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Products_Price] CHECK ([Price] >= 0),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderCode] nvarchar(20) NOT NULL,
    [UserId] nvarchar(500) NULL,
    [CustomerName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(100) NULL,
    [ShippingAddress] nvarchar(255) NOT NULL,
    [Province] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NOT NULL,
    [Ward] nvarchar(100) NOT NULL,
    [Note] nvarchar(500) NULL,
    [SubTotal] decimal(18,0) NOT NULL,
    [ShippingFee] decimal(18,0) NOT NULL,
    [DiscountAmount] decimal(18,0) NOT NULL,
    [CouponId] int NULL,
    [TotalAmount] decimal(18,0) NOT NULL,
    [PaymentMethod] tinyint NOT NULL,
    [PaymentStatus] tinyint NOT NULL,
    [Status] tinyint NOT NULL,
    [CancelReason] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Orders_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Orders_Coupons_CouponId] FOREIGN KEY ([CouponId]) REFERENCES [Coupons] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [GoodsReceipts] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [SupplierId] int NOT NULL,
    [CreatedByUserId] nvarchar(500) NOT NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [Status] tinyint NOT NULL,
    [TotalAmount] decimal(18,0) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    CONSTRAINT [PK_GoodsReceipts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GoodsReceipts_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_GoodsReceipts_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ProductImages] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ColorId] int NULL,
    [Url] nvarchar(500) NOT NULL,
    [IsMain] bit NOT NULL,
    [DisplayOrder] int NOT NULL,
    CONSTRAINT [PK_ProductImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductImages_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Colors] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ProductImages_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ProductVariants] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ColorId] int NOT NULL,
    [SizeId] int NOT NULL,
    [Sku] nvarchar(60) NOT NULL,
    [Price] decimal(18,0) NULL,
    [CostPrice] decimal(18,0) NOT NULL,
    [StockQuantity] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ProductVariants_Stock] CHECK ([StockQuantity] >= 0),
    CONSTRAINT [FK_ProductVariants_Colors_ColorId] FOREIGN KEY ([ColorId]) REFERENCES [Colors] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductVariants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductVariants_Sizes_SizeId] FOREIGN KEY ([SizeId]) REFERENCES [Sizes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PromotionProducts] (
    [PromotionId] int NOT NULL,
    [ProductId] int NOT NULL,
    CONSTRAINT [PK_PromotionProducts] PRIMARY KEY ([PromotionId], [ProductId]),
    CONSTRAINT [FK_PromotionProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PromotionProducts_Promotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [Promotions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Wishlists] (
    [UserId] nvarchar(500) NOT NULL,
    [ProductId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Wishlists] PRIMARY KEY ([UserId], [ProductId]),
    CONSTRAINT [FK_Wishlists_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Wishlists_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [OrderStatusHistories] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [FromStatus] tinyint NULL,
    [ToStatus] tinyint NOT NULL,
    [Note] nvarchar(255) NULL,
    [ChangedByUserId] nvarchar(500) NULL,
    [ChangedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OrderStatusHistories_AspNetUsers_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Reviews] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [UserId] nvarchar(500) NOT NULL,
    [OrderId] int NULL,
    [Rating] tinyint NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [IsApproved] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
    CONSTRAINT [FK_Reviews_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Reviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CartItems] (
    [Id] int NOT NULL IDENTITY,
    [CartId] int NOT NULL,
    [VariantId] int NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_CartItems] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_CartItems_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [FK_CartItems_Carts_CartId] FOREIGN KEY ([CartId]) REFERENCES [Carts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [GoodsReceiptDetails] (
    [Id] int NOT NULL IDENTITY,
    [GoodsReceiptId] int NOT NULL,
    [VariantId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitCost] decimal(18,0) NOT NULL,
    CONSTRAINT [PK_GoodsReceiptDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_GoodsReceiptDetails_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_GoodsReceiptDetails_UnitCost] CHECK ([UnitCost] >= 0),
    CONSTRAINT [FK_GoodsReceiptDetails_GoodsReceipts_GoodsReceiptId] FOREIGN KEY ([GoodsReceiptId]) REFERENCES [GoodsReceipts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GoodsReceiptDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [InventoryTransactions] (
    [Id] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [Type] tinyint NOT NULL,
    [Quantity] int NOT NULL,
    [StockAfter] int NOT NULL,
    [ReferenceType] nvarchar(30) NULL,
    [ReferenceId] int NULL,
    [Note] nvarchar(255) NULL,
    [CreatedByUserId] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransactions_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_InventoryTransactions_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [OrderDetails] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [VariantId] int NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [Sku] nvarchar(60) NOT NULL,
    [ColorName] nvarchar(50) NOT NULL,
    [SizeName] nvarchar(20) NOT NULL,
    [UnitPrice] decimal(18,0) NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_OrderDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_OrderDetails_Quantity] CHECK ([Quantity] > 0),
    CONSTRAINT [FK_OrderDetails_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderDetails_ProductVariants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [ProductVariants] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Addresses_UserId] ON [Addresses] ([UserId]);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_CartItems_CartId_VariantId] ON [CartItems] ([CartId], [VariantId]);
GO

CREATE INDEX [IX_CartItems_VariantId] ON [CartItems] ([VariantId]);
GO

CREATE UNIQUE INDEX [IX_Carts_UserId] ON [Carts] ([UserId]);
GO

CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);
GO

CREATE UNIQUE INDEX [IX_Categories_Slug] ON [Categories] ([Slug]);
GO

CREATE UNIQUE INDEX [IX_Colors_Name] ON [Colors] ([Name]);
GO

CREATE INDEX [IX_Contacts_HandledByUserId] ON [Contacts] ([HandledByUserId]);
GO

CREATE UNIQUE INDEX [IX_Coupons_Code] ON [Coupons] ([Code]);
GO

CREATE INDEX [IX_GoodsReceiptDetails_GoodsReceiptId] ON [GoodsReceiptDetails] ([GoodsReceiptId]);
GO

CREATE INDEX [IX_GoodsReceiptDetails_VariantId] ON [GoodsReceiptDetails] ([VariantId]);
GO

CREATE UNIQUE INDEX [IX_GoodsReceipts_Code] ON [GoodsReceipts] ([Code]);
GO

CREATE INDEX [IX_GoodsReceipts_CreatedByUserId] ON [GoodsReceipts] ([CreatedByUserId]);
GO

CREATE INDEX [IX_GoodsReceipts_SupplierId] ON [GoodsReceipts] ([SupplierId]);
GO

CREATE INDEX [IX_InventoryTransactions_CreatedByUserId] ON [InventoryTransactions] ([CreatedByUserId]);
GO

CREATE INDEX [IX_InventoryTransactions_VariantId_CreatedAt] ON [InventoryTransactions] ([VariantId], [CreatedAt]);
GO

CREATE INDEX [IX_OrderDetails_OrderId] ON [OrderDetails] ([OrderId]);
GO

CREATE INDEX [IX_OrderDetails_VariantId] ON [OrderDetails] ([VariantId]);
GO

CREATE INDEX [IX_Orders_CouponId] ON [Orders] ([CouponId]);
GO

CREATE UNIQUE INDEX [IX_Orders_OrderCode] ON [Orders] ([OrderCode]);
GO

CREATE INDEX [IX_Orders_Phone] ON [Orders] ([Phone]);
GO

CREATE INDEX [IX_Orders_Status_CreatedAt] ON [Orders] ([Status], [CreatedAt]);
GO

CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);
GO

CREATE INDEX [IX_OrderStatusHistories_ChangedByUserId] ON [OrderStatusHistories] ([ChangedByUserId]);
GO

CREATE INDEX [IX_OrderStatusHistories_OrderId] ON [OrderStatusHistories] ([OrderId]);
GO

CREATE INDEX [IX_Posts_AuthorId] ON [Posts] ([AuthorId]);
GO

CREATE UNIQUE INDEX [IX_Posts_Slug] ON [Posts] ([Slug]);
GO

CREATE INDEX [IX_ProductImages_ColorId] ON [ProductImages] ([ColorId]);
GO

CREATE INDEX [IX_ProductImages_ProductId] ON [ProductImages] ([ProductId]);
GO

CREATE INDEX [IX_Products_CategoryId_IsActive] ON [Products] ([CategoryId], [IsActive]);
GO

CREATE UNIQUE INDEX [IX_Products_Code] ON [Products] ([Code]);
GO

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
GO

CREATE UNIQUE INDEX [IX_Products_Slug] ON [Products] ([Slug]);
GO

CREATE INDEX [IX_ProductVariants_ColorId] ON [ProductVariants] ([ColorId]);
GO

CREATE INDEX [IX_ProductVariants_SizeId] ON [ProductVariants] ([SizeId]);
GO

CREATE UNIQUE INDEX [IX_ProductVariants_Sku] ON [ProductVariants] ([Sku]);
GO

CREATE UNIQUE INDEX [UQ_Variant] ON [ProductVariants] ([ProductId], [ColorId], [SizeId]);
GO

CREATE INDEX [IX_PromotionProducts_ProductId] ON [PromotionProducts] ([ProductId]);
GO

CREATE INDEX [IX_Reviews_OrderId] ON [Reviews] ([OrderId]);
GO

CREATE INDEX [IX_Reviews_ProductId_IsApproved] ON [Reviews] ([ProductId], [IsApproved]);
GO

CREATE INDEX [IX_Reviews_UserId] ON [Reviews] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_Sizes_Name] ON [Sizes] ([Name]);
GO

CREATE INDEX [IX_Wishlists_ProductId] ON [Wishlists] ([ProductId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260914121345_InitialCreate', N'8.0.31');
GO

COMMIT;
GO

