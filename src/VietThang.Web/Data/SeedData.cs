using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Data;

/// <summary>
/// Dữ liệu khởi tạo: vai trò, tài khoản admin, màu, size, danh mục, cấu hình, cửa hàng,
/// nhà cung cấp và một số sản phẩm mẫu theo cửa hàng Việt Thắng.
/// Chạy mỗi lần khởi động, chỉ chèn khi bảng còn trống.
/// </summary>
public static class SeedData
{
    public const string RoleAdmin = "Admin";
    public const string RoleEmployee = "Employee";
    public const string RoleCustomer = "Customer";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        var admin = await SeedUsersAsync(userManager);
        await SeedSettingsAsync(db);
        await SeedColorsAndSizesAsync(db);
        await SeedCategoriesAsync(db);
        await SeedStoresAsync(db);
        await SeedSuppliersAsync(db);
        await SeedBannersAsync(db);
        await SeedProductsAsync(db);
        await SeedPromotionsAndCouponsAsync(db);
        await SeedPostsAsync(db, admin);
        await UpgradeImagesAsync(db, services.GetRequiredService<IWebHostEnvironment>());
    }

    /// <summary>Đường dẫn ảnh minh họa theo mã sản phẩm + màu (sinh bởi tools/gen-images.mjs).</summary>
    private static string ProductImagePath(string code, string colorName) => $"/images/products/{code}-{SlugHelper.ToCodePart(colorName)}.svg";

    /// <summary>
    /// Chạy mỗi lần khởi động: sản phẩm chỉ có ảnh placeholder sẽ được gắn ảnh theo từng màu (nếu file tồn tại);
    /// danh mục gốc chưa có ảnh sẽ dùng ảnh /images/categories/{slug}.svg.
    /// </summary>
    private static async Task UpgradeImagesAsync(ApplicationDbContext db, IWebHostEnvironment env)
    {
        bool Exists(string url) => File.Exists(Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

        var products = await db.Products.Include(p => p.Images).Include(p => p.Variants).ThenInclude(v => v.Color).ToListAsync();
        foreach (var p in products)
        {
            if (p.Images.Any(i => !i.Url.EndsWith("placeholder.svg"))) continue;
            var colors = p.Variants.Select(v => v.Color).DistinctBy(c => c.Id).OrderBy(c => c.Name).ToList();
            var images = colors.Select(c => (Color: c, Url: ProductImagePath(p.Code, c.Name))).Where(x => Exists(x.Url)).ToList();
            if (images.Count == 0) continue;
            p.Images.Clear();
            int order = 0;
            foreach (var (color, url) in images)
                p.Images.Add(new ProductImage { Url = url, ColorId = color.Id, IsMain = order == 0, DisplayOrder = order++ });
        }

        foreach (var c in await db.Categories.Where(c => c.ParentId == null && c.ImageUrl == null).ToListAsync())
        {
            var url = $"/images/categories/{c.Slug}.svg";
            if (Exists(url)) c.ImageUrl = url;
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { RoleAdmin, RoleEmployee, RoleCustomer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<ApplicationUser> SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var admin = await EnsureUserAsync(userManager, "admin@thoitrangvietthang.vn", "Admin@123", "Quản trị viên", RoleAdmin);
        await EnsureUserAsync(userManager, "nhanvien@thoitrangvietthang.vn", "NhanVien@123", "Nguyễn Thị Hoa", RoleEmployee);
        await EnsureUserAsync(userManager, "khachhang@gmail.com", "KhachHang@123", "Trần Văn Khách", RoleCustomer);
        return admin;
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager,
        string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                PhoneNumber = "0900000000",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception("Không tạo được tài khoản seed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        if (await db.Settings.AnyAsync()) return;
        db.Settings.AddRange(
            new Setting { Key = SettingKeys.StoreName, Value = "Thời trang gia đình VT - Việt Thắng", Description = "Tên cửa hàng" },
            new Setting { Key = SettingKeys.Hotline, Value = "1800646859", Description = "Hotline" },
            new Setting { Key = SettingKeys.Email, Value = "info@thoitrangvietthang.vn", Description = "Email liên hệ" },
            new Setting { Key = SettingKeys.DefaultShippingFee, Value = "30000", Description = "Phí vận chuyển mặc định (đ)" },
            new Setting { Key = SettingKeys.FreeShippingThreshold, Value = "500000", Description = "Miễn phí vận chuyển cho đơn từ (đ)" },
            new Setting { Key = SettingKeys.LowStockThreshold, Value = "5", Description = "Ngưỡng cảnh báo sắp hết hàng" },
            new Setting { Key = SettingKeys.BankAccount, Value = "Vietcombank - STK 0011002233445 - CTK Cửa hàng Việt Thắng", Description = "Thông tin chuyển khoản" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedColorsAndSizesAsync(ApplicationDbContext db)
    {
        if (!await db.Colors.AnyAsync())
        {
            db.Colors.AddRange(
                new Color { Name = "Trắng", HexCode = "#FFFFFF" },
                new Color { Name = "Đen", HexCode = "#000000" },
                new Color { Name = "Xanh than", HexCode = "#1F3A5F" },
                new Color { Name = "Xanh mint", HexCode = "#98D8C8" },
                new Color { Name = "Hồng", HexCode = "#F4A7B9" },
                new Color { Name = "Be", HexCode = "#D9C3A5" },
                new Color { Name = "Đỏ đô", HexCode = "#7B1E3A" },
                new Color { Name = "Vàng", HexCode = "#F2C14E" },
                new Color { Name = "Tím", HexCode = "#8E6C9E" },
                new Color { Name = "Xám", HexCode = "#9AA0A6" });
        }
        if (!await db.Sizes.AnyAsync())
        {
            db.Sizes.AddRange(
                new Size { Name = "S", DisplayOrder = 1 },
                new Size { Name = "M", DisplayOrder = 2 },
                new Size { Name = "L", DisplayOrder = 3 },
                new Size { Name = "XL", DisplayOrder = 4 },
                new Size { Name = "XXL", DisplayOrder = 5 },
                new Size { Name = "3XL", DisplayOrder = 6 },
                new Size { Name = "Size 1 (1-2 tuổi)", DisplayOrder = 11 },
                new Size { Name = "Size 2 (3-4 tuổi)", DisplayOrder = 12 },
                new Size { Name = "Size 3 (5-6 tuổi)", DisplayOrder = 13 },
                new Size { Name = "Size 4 (7-8 tuổi)", DisplayOrder = 14 },
                new Size { Name = "Size 5 (9-10 tuổi)", DisplayOrder = 15 });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var nu = new Category { Name = "Nữ", Slug = "nu", DisplayOrder = 1 };
        var trungNien = new Category { Name = "Trung niên", Slug = "trung-nien", DisplayOrder = 2 };
        var treEm = new Category { Name = "Trẻ em", Slug = "tre-em", DisplayOrder = 3 };
        var nam = new Category { Name = "Nam", Slug = "nam", DisplayOrder = 4 };

        nu.Children = new List<Category>
        {
            new() { Name = "Bộ đồ quần đùi nữ", Slug = "bo-do-quan-dui-nu", DisplayOrder = 1 },
            new() { Name = "Bộ đồ quần lửng nữ", Slug = "bo-do-quan-lung-nu", DisplayOrder = 2 },
            new() { Name = "Bộ đồ quần dài nữ", Slug = "bo-do-quan-dai-nu", DisplayOrder = 3 },
            new() { Name = "Đồ lẻ nữ", Slug = "do-le-nu", DisplayOrder = 4 },
            new() { Name = "Đồ teen", Slug = "do-teen", DisplayOrder = 5 },
        };
        trungNien.Children = new List<Category>
        {
            new() { Name = "Bộ quần lửng trung niên", Slug = "bo-quan-lung-trung-nien", DisplayOrder = 1 },
            new() { Name = "Bộ quần dài trung niên", Slug = "bo-quan-dai-trung-nien", DisplayOrder = 2 },
            new() { Name = "Áo trung niên", Slug = "ao-trung-nien", DisplayOrder = 3 },
            new() { Name = "Chân váy trung niên", Slug = "chan-vay-trung-nien", DisplayOrder = 4 },
        };
        treEm.Children = new List<Category>
        {
            new() { Name = "Bé trai", Slug = "be-trai", DisplayOrder = 1 },
            new() { Name = "Bé gái", Slug = "be-gai", DisplayOrder = 2 },
        };
        nam.Children = new List<Category>
        {
            new() { Name = "Bộ đồ nam", Slug = "bo-do-nam", DisplayOrder = 1 },
            new() { Name = "Đồ lẻ nam", Slug = "do-le-nam", DisplayOrder = 2 },
        };

        db.Categories.AddRange(nu, trungNien, treEm, nam);
        await db.SaveChangesAsync();
    }

    private static async Task SeedStoresAsync(ApplicationDbContext db)
    {
        if (await db.Stores.AnyAsync()) return;
        db.Stores.AddRange(
            new Store { Name = "VT Cầu Giấy", Address = "Số 12 Cầu Giấy, Quận Cầu Giấy, Hà Nội", Phone = "1800646859", OpeningHours = "8:30 - 21:30" },
            new Store { Name = "VT Hà Đông", Address = "Số 88 Quang Trung, Quận Hà Đông, Hà Nội", Phone = "1800646859", OpeningHours = "8:30 - 21:30" },
            new Store { Name = "VT Long Biên", Address = "Số 25 Nguyễn Văn Cừ, Quận Long Biên, Hà Nội", Phone = "1800646859", OpeningHours = "8:30 - 21:30" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedSuppliersAsync(ApplicationDbContext db)
    {
        if (await db.Suppliers.AnyAsync()) return;
        db.Suppliers.AddRange(
            new Supplier { Name = "Xưởng may Minh Anh", ContactName = "Chị Minh Anh", Phone = "0912345678", Email = "minhanh@xuongmay.vn", Address = "Thường Tín, Hà Nội" },
            new Supplier { Name = "Công ty Dệt Lanh Việt", ContactName = "Anh Tuấn", Phone = "0987654321", Email = "lanhviet@detmay.vn", Address = "Nam Định" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedBannersAsync(ApplicationDbContext db)
    {
        if (await db.Banners.AnyAsync()) return;
        db.Banners.AddRange(
            new Banner { Title = "Bộ sưu tập lanh mùa hè", ImageUrl = "/images/banners/banner-1.svg", LinkUrl = "/danh-muc/nu", Position = BannerPosition.HomeSlider, DisplayOrder = 1 },
            new Banner { Title = "Sale đến 30%", ImageUrl = "/images/banners/banner-2.svg", LinkUrl = "/sale", Position = BannerPosition.HomeSlider, DisplayOrder = 2 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        var cats = await db.Categories.ToDictionaryAsync(c => c.Slug);
        var colors = await db.Colors.ToDictionaryAsync(c => c.Name);
        var sizes = await db.Sizes.ToDictionaryAsync(s => s.Name);

        var adultSizes = new[] { "S", "M", "L", "XL", "XXL" };
        var kidSizes = new[] { "Size 1 (1-2 tuổi)", "Size 2 (3-4 tuổi)", "Size 3 (5-6 tuổi)", "Size 4 (7-8 tuổi)" };

        var samples = new (string code, string name, string slug, string cat, string material, decimal price, bool isNew, string[] colorNames, string[] sizeNames)[]
        {
            ("VT-BL-001", "Bộ lanh nữ quần đùi họa tiết hoa nhí", "bo-lanh-nu-quan-dui-hoa-nhi", "bo-do-quan-dui-nu", "Lanh", 275000, true, new[] { "Xanh mint", "Hồng", "Be" }, adultSizes),
            ("VT-BL-002", "Bộ lanh nữ quần lửng cổ tròn", "bo-lanh-nu-quan-lung-co-tron", "bo-do-quan-lung-nu", "Lanh", 305000, true, new[] { "Xanh than", "Đỏ đô", "Trắng" }, adultSizes),
            ("VT-CT-003", "Bộ cotton nữ quần dài tay lỡ", "bo-cotton-nu-quan-dai-tay-lo", "bo-do-quan-dai-nu", "Cotton", 415000, false, new[] { "Xám", "Đen", "Tím" }, adultSizes),
            ("VT-TL-004", "Áo thun lạnh nữ cổ tim", "ao-thun-lanh-nu-co-tim", "do-le-nu", "Thun lạnh", 145000, false, new[] { "Trắng", "Đen", "Hồng", "Vàng" }, adultSizes),
            ("VT-TN-005", "Bộ lanh trung niên quần lửng in hoa", "bo-lanh-trung-nien-quan-lung-in-hoa", "bo-quan-lung-trung-nien", "Lanh", 320000, true, new[] { "Xanh than", "Đỏ đô", "Tím" }, new[] { "L", "XL", "XXL", "3XL" }),
            ("VT-TN-006", "Bộ lanh trung niên quần dài cổ sen", "bo-lanh-trung-nien-quan-dai-co-sen", "bo-quan-dai-trung-nien", "Lanh", 365000, false, new[] { "Be", "Xám", "Xanh than" }, new[] { "L", "XL", "XXL", "3XL" }),
            ("VT-BT-007", "Bộ cotton bé trai in khủng long", "bo-cotton-be-trai-in-khung-long", "be-trai", "Cotton", 165000, true, new[] { "Xanh than", "Xám", "Vàng" }, kidSizes),
            ("VT-BG-008", "Bộ lanh bé gái hoa nhí cổ bèo", "bo-lanh-be-gai-hoa-nhi-co-beo", "be-gai", "Lanh", 175000, false, new[] { "Hồng", "Xanh mint", "Trắng" }, kidSizes),
            ("VT-NM-009", "Bộ thun nam cổ tròn quần đùi", "bo-thun-nam-co-tron-quan-dui", "bo-do-nam", "Thun lạnh", 235000, true, new[] { "Đen", "Xám", "Xanh than" }, adultSizes),
            ("VT-NM-010", "Quần lửng nam lanh lưng thun", "quan-lung-nam-lanh-lung-thun", "do-le-nam", "Lanh", 155000, false, new[] { "Be", "Xám", "Đen" }, adultSizes),
        };

        var rnd = new Random(2024);
        foreach (var s in samples)
        {
            var product = new Product
            {
                Code = s.code,
                Name = s.name,
                Slug = s.slug,
                CategoryId = cats[s.cat].Id,
                Material = s.material,
                Price = s.price,
                IsNew = s.isNew,
                IsFeatured = s.isNew,
                ShortDescription = $"Chất liệu {s.material.ToLower()} mềm mát, thoáng khí, phù hợp mặc nhà và đi chơi.",
                Description = $"<p><strong>{s.name}</strong> được may từ chất liệu {s.material.ToLower()} cao cấp, thấm hút tốt, không xù lông sau nhiều lần giặt.</p><ul><li>Form dáng thoải mái, dễ mặc.</li><li>Đường may tỉ mỉ, chắc chắn.</li><li>Sản xuất tại xưởng của Việt Thắng.</li></ul>"
            };
            int imageOrder = 0;
            foreach (var colorName in s.colorNames)
            {
                var color = colors[colorName];
                product.Images.Add(new ProductImage { Url = ProductImagePath(s.code, colorName), ColorId = color.Id, IsMain = imageOrder == 0, DisplayOrder = imageOrder++ });
                foreach (var sizeName in s.sizeNames)
                {
                    var size = sizes[sizeName];
                    product.Variants.Add(new ProductVariant
                    {
                        ColorId = color.Id,
                        SizeId = size.Id,
                        Sku = $"{s.code}-{SlugHelper.ToCodePart(colorName)}-{SlugHelper.ToCodePart(sizeName)}",
                        CostPrice = Math.Round(s.price * 0.6m / 1000) * 1000,
                        StockQuantity = rnd.Next(5, 40)
                    });
                }
            }
            db.Products.Add(product);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedPromotionsAndCouponsAsync(ApplicationDbContext db)
    {
        if (!await db.Promotions.AnyAsync())
        {
            var saleProducts = await db.Products.Where(p => p.Code.StartsWith("VT-BL") || p.Code == "VT-TN-005").ToListAsync();
            var promo = new Promotion
            {
                Name = "Sale hè - Giảm 30% đồ lanh",
                DiscountType = DiscountType.Percent,
                DiscountValue = 30,
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today.AddMonths(2)
            };
            foreach (var p in saleProducts)
                promo.PromotionProducts.Add(new PromotionProduct { ProductId = p.Id });
            db.Promotions.Add(promo);
        }
        if (!await db.Coupons.AnyAsync())
        {
            db.Coupons.AddRange(
                new Coupon { Code = "VT10", Description = "Giảm 10% cho đơn từ 300.000đ, tối đa 50.000đ", DiscountType = DiscountType.Percent, DiscountValue = 10, MaxDiscountAmount = 50000, MinOrderAmount = 300000, UsageLimit = 100, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3) },
                new Coupon { Code = "GIAM30K", Description = "Giảm 30.000đ cho đơn từ 200.000đ", DiscountType = DiscountType.Amount, DiscountValue = 30000, MinOrderAmount = 200000, UsageLimit = 200, StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3) });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedPostsAsync(ApplicationDbContext db, ApplicationUser author)
    {
        if (await db.Posts.AnyAsync()) return;
        db.Posts.AddRange(
            new Post
            {
                Title = "Cách bảo quản đồ lanh luôn mềm và bền màu",
                Slug = "cach-bao-quan-do-lanh",
                Summary = "Vài mẹo nhỏ giúp bộ đồ lanh của bạn giữ được độ mềm mát và màu sắc lâu dài.",
                Content = "<p>Giặt bằng nước lạnh, không dùng chất tẩy mạnh, phơi trong bóng râm và ủi ở nhiệt độ vừa khi vải còn hơi ẩm.</p>",
                Type = PostType.News, AuthorId = author.Id, IsPublished = true, PublishedAt = DateTime.Now
            },
            new Post
            {
                Title = "Tuyển nhân viên bán hàng tại Cầu Giấy",
                Slug = "tuyen-nhan-vien-ban-hang-cau-giay",
                Summary = "Cửa hàng VT Cầu Giấy tuyển 2 nhân viên bán hàng, làm theo ca, lương 7-9 triệu.",
                Content = "<p>Yêu cầu: nhanh nhẹn, giao tiếp tốt, ưu tiên có kinh nghiệm bán thời trang. Liên hệ hotline 1800 6468 59.</p>",
                Type = PostType.Recruitment, AuthorId = author.Id, IsPublished = true, PublishedAt = DateTime.Now
            });
        await db.SaveChangesAsync();
    }

}
