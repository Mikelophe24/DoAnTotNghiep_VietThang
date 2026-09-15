using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Models.Entities;

namespace VietThang.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Người dùng
    public DbSet<Address> Addresses => Set<Address>();

    // Danh mục & sản phẩm
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    // Khuyến mãi
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    // Giỏ hàng & đơn hàng
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    // Kho
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptDetail> GoodsReceiptDetails => Set<GoodsReceiptDetail>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    // Tương tác
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Contact> Contacts => Set<Contact>();

    // Nội dung & cấu hình
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // VNĐ không có phần lẻ
        builder.Properties<decimal>().HavePrecision(18, 0);
        builder.Properties<string>().HaveMaxLength(500);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---------- Người dùng ----------
        b.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
        });

        b.Entity<Address>(e =>
        {
            e.Property(x => x.ReceiverName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100).IsRequired();
            e.Property(x => x.District).HasMaxLength(100).IsRequired();
            e.Property(x => x.Ward).HasMaxLength(100).IsRequired();
            e.Property(x => x.Street).HasMaxLength(255).IsRequired();
            e.Ignore(x => x.FullAddress);
            e.HasOne(x => x.User).WithMany(u => u.Addresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Danh mục & sản phẩm ----------
        b.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Color>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.HexCode).HasMaxLength(7);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Size>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Product>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            e.Property(x => x.Material).HasMaxLength(100);
            e.Property(x => x.Description).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => new { x.CategoryId, x.IsActive });
            e.HasIndex(x => x.Name);
            e.HasOne(x => x.Category).WithMany(c => c.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Products_Price", "[Price] >= 0"));
        });

        b.Entity<ProductImage>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
            e.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Color).WithMany().HasForeignKey(x => x.ColorId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ProductVariant>(e =>
        {
            e.Property(x => x.Sku).HasMaxLength(60).IsRequired();
            e.HasIndex(x => x.Sku).IsUnique();
            e.HasIndex(x => new { x.ProductId, x.ColorId, x.SizeId }).IsUnique().HasDatabaseName("UQ_Variant");
            e.HasOne(x => x.Product).WithMany(p => p.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Color).WithMany(c => c.Variants).HasForeignKey(x => x.ColorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Size).WithMany(s => s.Variants).HasForeignKey(x => x.SizeId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_ProductVariants_Stock", "[StockQuantity] >= 0"));
        });

        // ---------- Khuyến mãi ----------
        b.Entity<Promotion>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Promotions_Date", "[EndDate] > [StartDate]"));
        });

        b.Entity<PromotionProduct>(e =>
        {
            e.HasKey(x => new { x.PromotionId, x.ProductId });
            e.HasOne(x => x.Promotion).WithMany(p => p.PromotionProducts).HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(p => p.PromotionProducts).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Coupon>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(30).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ---------- Giỏ hàng & đơn hàng ----------
        b.Entity<Cart>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.Cart).HasForeignKey<Cart>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CartItem>(e =>
        {
            e.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();
            e.HasOne(x => x.Cart).WithMany(c => c.Items).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Variant).WithMany(v => v.CartItems).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] > 0"));
        });

        b.Entity<Order>(e =>
        {
            e.Property(x => x.OrderCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.ShippingAddress).HasMaxLength(255).IsRequired();
            e.Property(x => x.Province).HasMaxLength(100).IsRequired();
            e.Property(x => x.District).HasMaxLength(100).IsRequired();
            e.Property(x => x.Ward).HasMaxLength(100).IsRequired();
            e.Property(x => x.CancelReason).HasMaxLength(255);
            e.HasIndex(x => x.OrderCode).IsUnique();
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Phone);
            e.HasOne(x => x.User).WithMany(u => u.Orders).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Coupon).WithMany(c => c.Orders).HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<OrderDetail>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(60).IsRequired();
            e.Property(x => x.ColorName).HasMaxLength(50).IsRequired();
            e.Property(x => x.SizeName).HasMaxLength(20).IsRequired();
            e.Ignore(x => x.LineTotal);
            e.HasIndex(x => x.VariantId);
            e.HasOne(x => x.Order).WithMany(o => o.Details).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Variant).WithMany(v => v.OrderDetails).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_OrderDetails_Quantity", "[Quantity] > 0"));
        });

        b.Entity<OrderStatusHistory>(e =>
        {
            e.Property(x => x.Note).HasMaxLength(255);
            e.HasOne(x => x.Order).WithMany(o => o.StatusHistories).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedBy).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- Kho ----------
        b.Entity<Supplier>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.Address).HasMaxLength(255);
        });

        b.Entity<GoodsReceipt>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasOne(x => x.Supplier).WithMany(s => s.GoodsReceipts).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<GoodsReceiptDetail>(e =>
        {
            e.Ignore(x => x.LineTotal);
            e.HasOne(x => x.GoodsReceipt).WithMany(r => r.Details).HasForeignKey(x => x.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Variant).WithMany(v => v.GoodsReceiptDetails).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_GoodsReceiptDetails_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_GoodsReceiptDetails_UnitCost", "[UnitCost] >= 0");
            });
        });

        b.Entity<InventoryTransaction>(e =>
        {
            e.Property(x => x.ReferenceType).HasMaxLength(30);
            e.Property(x => x.Note).HasMaxLength(255);
            e.HasIndex(x => new { x.VariantId, x.CreatedAt });
            e.HasOne(x => x.Variant).WithMany(v => v.InventoryTransactions).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- Tương tác ----------
        b.Entity<Review>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(1000);
            e.HasIndex(x => new { x.ProductId, x.IsApproved });
            e.HasOne(x => x.Product).WithMany(p => p.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Reviews).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
        });

        b.Entity<Wishlist>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ProductId });
            e.HasOne(x => x.User).WithMany(u => u.Wishlists).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(p => p.Wishlists).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Contact>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Subject).HasMaxLength(200);
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.HasOne(x => x.HandledBy).WithMany().HasForeignKey(x => x.HandledByUserId).OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- Nội dung & cấu hình ----------
        b.Entity<Post>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(220).IsRequired();
            e.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Banner>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(150);
            e.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        });

        b.Entity<Store>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Address).HasMaxLength(255).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.OpeningHours).HasMaxLength(100);
            e.Property(x => x.MapEmbedUrl).HasMaxLength(1000);
        });

        b.Entity<Setting>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(50);
            e.Property(x => x.Value).HasColumnType("nvarchar(max)");
            e.Property(x => x.Description).HasMaxLength(200);
        });
    }
}
