using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Web.Data;

/// <summary>
/// Sinh dữ liệu demo cho báo cáo và bảo vệ đồ án: 10 sản phẩm bổ sung, 15 khách hàng, 4 phiếu nhập,
/// khoảng 80 đơn hàng trải 90 ngày gần nhất (đủ trạng thái), đánh giá, yêu thích, liên hệ.
/// Chạy một lần bằng:  dotnet run -- --seed-demo
/// Dữ liệu sinh theo hạt giống cố định nên kết quả lặp lại được; ngày tháng tính lùi từ hôm chạy.
/// </summary>
public static class DemoDataSeeder
{
    public const string MarkerKey = "DemoDataSeededAt";
    private const string DemoPassword = "KhachHang@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var pricing = services.GetRequiredService<IPricingService>();
        var settings = services.GetRequiredService<ISettingService>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");

        if (await db.Settings.AnyAsync(s => s.Key == MarkerKey))
        {
            logger.LogWarning("Dữ liệu demo đã được tạo trước đó, bỏ qua. Xóa dòng Settings '{Key}' nếu muốn tạo thêm.", MarkerKey);
            return;
        }

        var rnd = new Random(20261003);
        var now = DateTime.Now;
        var today = now.Date;

        await using var tx = await db.Database.BeginTransactionAsync();

        await SeedProductsAsync(db);
        await SeedPromotionAsync(db, today);
        var customers = await SeedCustomersAsync(userManager, db, rnd, today);
        var staff = await userManager.FindByEmailAsync("nhanvien@thoitrangvietthang.vn")
                    ?? (await userManager.GetUsersInRoleAsync(SeedData.RoleAdmin)).First();

        var variants = await db.ProductVariants.Include(v => v.Product).Include(v => v.Color).Include(v => v.Size)
            .Where(v => v.IsActive && v.Product.IsActive).ToListAsync();
        var promos = await pricing.GetActivePromotionsAsync(variants.Select(v => v.ProductId));
        var suppliers = await db.Suppliers.ToListAsync();
        var coupons = await db.Coupons.Where(c => c.Code == "VT10" || c.Code == "GIAM30K").ToDictionaryAsync(c => c.Code);
        var shippingFee = await settings.GetDecimalAsync(SettingKeys.DefaultShippingFee, 30000);
        var freeShip = await settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000);

        var orderSeq = (await db.Orders.Select(o => o.OrderCode).ToListAsync())
            .GroupBy(c => c.Substring(0, 8)).ToDictionary(g => g.Key, g => g.Max(c => int.Parse(c.Substring(8))));
        var receiptSeq = (await db.GoodsReceipts.Select(r => r.Code).ToListAsync())
            .GroupBy(c => c.Substring(0, 8)).ToDictionary(g => g.Key, g => g.Max(c => int.Parse(c.Substring(8))));

        var receiptDays = new HashSet<int> { 86, 62, 38, 14 };
        var completedOrders = new List<Order>();
        int orderCount = 0, receiptCount = 0;

        // Mô phỏng theo dòng thời gian để tồn kho và nhật ký kho nhất quán
        for (int offset = 89; offset >= 0; offset--)
        {
            var date = today.AddDays(-offset);

            if (receiptDays.Contains(offset))
            {
                var first = offset == 86;
                var targets = first
                    ? variants.Where(v => v.StockQuantity == 0).Concat(variants.Where(v => v.StockQuantity > 0).OrderBy(_ => rnd.Next()).Take(20)).ToList()
                    : variants.OrderBy(v => v.StockQuantity).ThenBy(_ => rnd.Next()).Take(30).ToList();
                var receipt = new GoodsReceipt
                {
                    Code = NextCode("PN", date, receiptSeq),
                    SupplierId = suppliers[receiptCount % suppliers.Count].Id,
                    CreatedByUserId = staff.Id,
                    ReceiptDate = date.AddHours(9),
                    CreatedAt = date.AddHours(8).AddMinutes(40),
                    CompletedAt = date.AddHours(9).AddMinutes(15),
                    Status = ReceiptStatus.Completed,
                    Note = first ? "Nhập hàng đầu mùa" : "Nhập bổ sung hàng bán chạy"
                };
                foreach (var v in targets)
                {
                    var qty = rnd.Next(first ? 15 : 10, first ? 31 : 26);
                    var cost = v.CostPrice > 0 ? v.CostPrice : Math.Round((v.Price ?? v.Product.Price) * 0.6m / 1000) * 1000;
                    receipt.Details.Add(new GoodsReceiptDetail { VariantId = v.Id, Quantity = qty, UnitCost = cost });
                    v.StockQuantity += qty;
                    v.CostPrice = cost;
                }
                receipt.TotalAmount = receipt.Details.Sum(d => d.Quantity * d.UnitCost);
                db.GoodsReceipts.Add(receipt);
                await db.SaveChangesAsync();
                var stockNow = variants.ToDictionary(v => v.Id, v => v.StockQuantity);
                db.InventoryTransactions.AddRange(receipt.Details.Select(d => new InventoryTransaction
                {
                    VariantId = d.VariantId, Type = InventoryType.Import, Quantity = d.Quantity, StockAfter = stockNow[d.VariantId],
                    ReferenceType = "GoodsReceipt", ReferenceId = receipt.Id, Note = $"Nhập kho phiếu {receipt.Code}",
                    CreatedByUserId = staff.Id, CreatedAt = receipt.CompletedAt!.Value
                }));
                receiptCount++;
            }

            // Số đơn trong ngày tăng dần về gần hiện tại, cuối tuần nhiều hơn
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var p = 0.5 + 0.3 * (1 - offset / 90.0) + (weekend ? 0.15 : 0);
            var n = (rnd.NextDouble() < p ? 1 : 0) + (rnd.NextDouble() < p - 0.35 ? 1 : 0);

            for (int i = 0; i < n; i++)
            {
                var createdAt = date.AddHours(rnd.Next(8, 22)).AddMinutes(rnd.Next(0, 60));
                if (createdAt > now) createdAt = now.AddMinutes(-rnd.Next(5, 90));

                var registered = customers.Where(c => c.User.CreatedAt <= createdAt).ToList();
                var useAccount = registered.Count > 0 && rnd.NextDouble() < 0.65;
                var (name, phone, email, userId, addr) = useAccount
                    ? Pick(registered, rnd) is var c ? (c.User.FullName, c.User.PhoneNumber!, c.User.Email, (string?)c.User.Id, c.Address) : default
                    : (GuestNames[rnd.Next(GuestNames.Length)], "09" + rnd.Next(10000000, 99999999), (string?)null, (string?)null, Addresses[rnd.Next(Addresses.Length)]);

                var order = new Order
                {
                    OrderCode = NextCode("VT", date, orderSeq),
                    UserId = userId, CustomerName = name, Phone = phone, Email = email,
                    ShippingAddress = addr.Street, Province = addr.Province, District = addr.District, Ward = addr.Ward,
                    Note = rnd.NextDouble() < 0.2 ? OrderNotes[rnd.Next(OrderNotes.Length)] : null,
                    PaymentMethod = rnd.NextDouble() < 0.75 ? PaymentMethod.Cod : PaymentMethod.BankTransfer,
                    CreatedAt = createdAt
                };

                var lineCount = rnd.NextDouble() < 0.5 ? 1 : (rnd.NextDouble() < 0.7 ? 2 : 3);
                var logs = new List<InventoryTransaction>();
                foreach (var v in variants.Where(v => v.StockQuantity > 0).OrderBy(_ => rnd.Next()).Take(lineCount))
                {
                    var qty = Math.Min(v.StockQuantity, rnd.NextDouble() < 0.8 ? 1 : 2);
                    var price = pricing.Calculate(v.Price ?? v.Product.Price, promos.GetValueOrDefault(v.ProductId)).SalePrice;
                    order.Details.Add(new OrderDetail
                    {
                        VariantId = v.Id, ProductName = v.Product.Name, Sku = v.Sku, ColorName = v.Color.Name, SizeName = v.Size.Name,
                        UnitPrice = price, UnitCost = v.CostPrice, Quantity = qty
                    });
                    v.StockQuantity -= qty;
                    logs.Add(new InventoryTransaction
                    {
                        VariantId = v.Id, Type = InventoryType.Export, Quantity = -qty, StockAfter = v.StockQuantity,
                        ReferenceType = "Order", Note = $"Xuất bán đơn {order.OrderCode}", CreatedByUserId = userId, CreatedAt = createdAt
                    });
                }
                if (order.Details.Count == 0) continue;

                order.SubTotal = order.Details.Sum(d => d.UnitPrice * d.Quantity);
                order.ShippingFee = order.SubTotal >= freeShip ? 0 : shippingFee;
                if (rnd.NextDouble() < 0.18)
                {
                    if (order.SubTotal >= 300000 && coupons.TryGetValue("VT10", out var vt10) && vt10.UsedCount < (vt10.UsageLimit ?? int.MaxValue))
                    { order.CouponId = vt10.Id; order.DiscountAmount = Math.Min(Math.Round(order.SubTotal * 0.1m), 50000); vt10.UsedCount++; }
                    else if (order.SubTotal >= 200000 && coupons.TryGetValue("GIAM30K", out var g30) && g30.UsedCount < (g30.UsageLimit ?? int.MaxValue))
                    { order.CouponId = g30.Id; order.DiscountAmount = 30000; g30.UsedCount++; }
                }
                order.TotalAmount = order.SubTotal + order.ShippingFee - order.DiscountAmount;

                // Trạng thái theo tuổi đơn
                var roll = rnd.NextDouble();
                var status = offset >= 6 ? (roll < 0.88 ? OrderStatus.Completed : OrderStatus.Cancelled)
                    : offset >= 3 ? (roll < 0.5 ? OrderStatus.Completed : roll < 0.85 ? OrderStatus.Shipping : OrderStatus.Cancelled)
                    : offset >= 1 ? (roll < 0.55 ? OrderStatus.Shipping : OrderStatus.Confirmed)
                    : (roll < 0.6 ? OrderStatus.Pending : OrderStatus.Confirmed);

                order.StatusHistories.Add(new OrderStatusHistory { ToStatus = OrderStatus.Pending, Note = "Khách đặt hàng", ChangedByUserId = userId, ChangedAt = createdAt });
                var t = createdAt;
                if (status == OrderStatus.Cancelled)
                {
                    t = Min(t.AddHours(rnd.Next(2, 20)), now);
                    var byCustomer = userId is not null && rnd.NextDouble() < 0.5;
                    order.CancelReason = byCustomer ? "Khách tự hủy: " + CustomerCancelReasons[rnd.Next(CustomerCancelReasons.Length)] : StaffCancelReasons[rnd.Next(StaffCancelReasons.Length)];
                    order.StatusHistories.Add(new OrderStatusHistory { FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.Cancelled, Note = order.CancelReason, ChangedByUserId = byCustomer ? userId : staff.Id, ChangedAt = t });
                    foreach (var d in order.Details)
                    {
                        var v = variants.First(x => x.Id == d.VariantId);
                        v.StockQuantity += d.Quantity;
                        logs.Add(new InventoryTransaction
                        {
                            VariantId = v.Id, Type = InventoryType.Return, Quantity = d.Quantity, StockAfter = v.StockQuantity,
                            ReferenceType = "Order", Note = $"Hoàn tồn do hủy đơn {order.OrderCode}", CreatedByUserId = byCustomer ? userId : staff.Id, CreatedAt = t
                        });
                    }
                    if (order.CouponId.HasValue) { var cp = coupons.Values.First(x => x.Id == order.CouponId); if (cp.UsedCount > 0) cp.UsedCount--; }
                }
                else
                {
                    if (status >= OrderStatus.Confirmed)
                    {
                        t = Min(t.AddHours(rnd.Next(1, 6)), now);
                        order.StatusHistories.Add(new OrderStatusHistory { FromStatus = OrderStatus.Pending, ToStatus = OrderStatus.Confirmed, Note = "Đã gọi xác nhận", ChangedByUserId = staff.Id, ChangedAt = t });
                        if (order.PaymentMethod == PaymentMethod.BankTransfer) order.PaymentStatus = PaymentStatus.Paid;
                    }
                    if (status >= OrderStatus.Shipping)
                    {
                        t = Min(t.AddHours(rnd.Next(12, 30)), now);
                        order.StatusHistories.Add(new OrderStatusHistory { FromStatus = OrderStatus.Confirmed, ToStatus = OrderStatus.Shipping, Note = "Giao cho đơn vị vận chuyển", ChangedByUserId = staff.Id, ChangedAt = t });
                    }
                    if (status == OrderStatus.Completed)
                    {
                        t = Min(t.AddDays(rnd.Next(1, 4)).AddHours(rnd.Next(0, 8)), now.AddMinutes(-10));
                        order.StatusHistories.Add(new OrderStatusHistory { FromStatus = OrderStatus.Shipping, ToStatus = OrderStatus.Completed, Note = "Giao thành công", ChangedByUserId = staff.Id, ChangedAt = t });
                        order.CompletedAt = t;
                        order.PaymentStatus = PaymentStatus.Paid;
                        completedOrders.Add(order);
                    }
                    foreach (var d in order.Details) variants.First(x => x.Id == d.VariantId).Product.SoldCount += d.Quantity;
                }
                order.Status = status;

                db.Orders.Add(order);
                await db.SaveChangesAsync();
                foreach (var log in logs) log.ReferenceId = order.Id;
                db.InventoryTransactions.AddRange(logs);
                orderCount++;
            }
            await db.SaveChangesAsync();
        }

        var reviewCount = SeedReviews(db, completedOrders, variants, rnd, now);
        SeedWishlists(db, customers, variants, rnd);
        SeedContacts(db, staff, today);
        db.Settings.Add(new Setting { Key = MarkerKey, Value = now.ToString("yyyy-MM-dd HH:mm"), Description = "Thời điểm tạo dữ liệu demo" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation("Đã tạo dữ liệu demo: {Customers} khách hàng, {Receipts} phiếu nhập, {Orders} đơn hàng, {Reviews} đánh giá.",
            customers.Count, receiptCount, orderCount, reviewCount);
    }

    // ---------------------------------------------------------------- sản phẩm bổ sung
    private static async Task SeedProductsAsync(ApplicationDbContext db)
    {
        var cats = await db.Categories.ToDictionaryAsync(c => c.Slug);
        var colors = await db.Colors.ToDictionaryAsync(c => c.Name);
        var sizes = await db.Sizes.ToDictionaryAsync(s => s.Name);
        var existing = (await db.Products.Select(p => p.Code).ToListAsync()).ToHashSet();

        var adult = new[] { "S", "M", "L", "XL", "XXL" };
        var middle = new[] { "L", "XL", "XXL", "3XL" };
        var kid = new[] { "Size 1 (1-2 tuổi)", "Size 2 (3-4 tuổi)", "Size 3 (5-6 tuổi)", "Size 4 (7-8 tuổi)" };

        var items = new (string Code, string Name, string Cat, string Material, decimal Price, bool IsNew, string[] Colors, string[] Sizes)[]
        {
            ("VT-BL-011", "Bộ lanh nữ quần dài cổ vuông", "bo-do-quan-dai-nu", "Lanh", 345000, true, new[] { "Be", "Xanh mint", "Hồng" }, adult),
            ("VT-BD-012", "Bộ đũi nữ quần đùi tay cánh tiên", "bo-do-quan-dui-nu", "Đũi", 255000, true, new[] { "Trắng", "Vàng", "Tím" }, adult),
            ("VT-TE-013", "Bộ thun teen in chữ quần lửng", "do-teen", "Cotton", 225000, false, new[] { "Đen", "Trắng", "Hồng" }, new[] { "S", "M", "L" }),
            ("VT-QN-014", "Quần lanh nữ ống rộng lưng thun", "do-le-nu", "Lanh", 185000, false, new[] { "Be", "Đen", "Xanh than" }, adult),
            ("VT-TN-015", "Áo lanh trung niên cổ tàu tay lỡ", "ao-trung-nien", "Lanh", 215000, false, new[] { "Đỏ đô", "Tím", "Xanh than" }, middle),
            ("VT-TN-016", "Chân váy lanh trung niên xếp ly", "chan-vay-trung-nien", "Lanh", 245000, true, new[] { "Đen", "Be", "Xanh than" }, middle),
            ("VT-BT-017", "Bộ thun bé trai sọc ngang quần đùi", "be-trai", "Cotton", 155000, false, new[] { "Xanh than", "Đỏ đô", "Xám" }, kid),
            ("VT-BG-018", "Váy lanh bé gái hai dây hoa nhí", "be-gai", "Lanh", 185000, true, new[] { "Hồng", "Vàng", "Trắng" }, kid),
            ("VT-NM-019", "Bộ lanh nam cổ tàu quần dài", "bo-do-nam", "Lanh", 395000, true, new[] { "Be", "Xám", "Xanh than" }, adult),
            ("VT-NM-020", "Áo thun lạnh nam cổ tròn", "do-le-nam", "Thun lạnh", 135000, false, new[] { "Trắng", "Đen", "Xám", "Xanh than" }, adult),
        };

        foreach (var s in items.Where(i => !existing.Contains(i.Code)))
        {
            var product = new Product
            {
                Code = s.Code, Name = s.Name, Slug = SlugHelper.ToSlug(s.Name), SearchText = SlugHelper.ToSearchText(s.Name, s.Code, s.Material),
                CategoryId = cats[s.Cat].Id, Material = s.Material, Price = s.Price, IsNew = s.IsNew, IsFeatured = s.IsNew,
                ShortDescription = $"Chất liệu {s.Material.ToLower()} mềm mát, thoáng khí, phù hợp mặc nhà và đi chơi.",
                Description = $"<p><strong>{s.Name}</strong> được may từ chất liệu {s.Material.ToLower()} cao cấp, thấm hút tốt, không xù lông sau nhiều lần giặt.</p><ul><li>Form dáng thoải mái, dễ mặc.</li><li>Đường may tỉ mỉ, chắc chắn.</li><li>Sản xuất tại xưởng của Việt Thắng.</li></ul>",
                CreatedAt = DateTime.Now.AddDays(-87)
            };
            int order = 0;
            foreach (var colorName in s.Colors)
            {
                var color = colors[colorName];
                product.Images.Add(new ProductImage { Url = $"/images/products/{s.Code}-{SlugHelper.ToCodePart(colorName)}.svg", ColorId = color.Id, IsMain = order == 0, DisplayOrder = order++ });
                foreach (var sizeName in s.Sizes)
                    product.Variants.Add(new ProductVariant
                    {
                        ColorId = color.Id, SizeId = sizes[sizeName].Id,
                        Sku = $"{s.Code}-{SlugHelper.ToCodePart(colorName)}-{SlugHelper.ToCodePart(sizeName)}",
                        CostPrice = Math.Round(s.Price * 0.6m / 1000) * 1000,
                        StockQuantity = 0   // tồn đến từ phiếu nhập đầu tiên
                    });
            }
            db.Products.Add(product);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedPromotionAsync(ApplicationDbContext db, DateTime today)
    {
        const string name = "Ưu đãi hàng mới - Giảm 10%";
        if (await db.Promotions.AnyAsync(p => p.Name == name)) return;
        var codes = new[] { "VT-BL-011", "VT-BD-012", "VT-NM-019" };
        var promo = new Promotion { Name = name, DiscountType = DiscountType.Percent, DiscountValue = 10, StartDate = today.AddDays(-20), EndDate = today.AddDays(40) };
        foreach (var id in await db.Products.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToListAsync())
            promo.PromotionProducts.Add(new PromotionProduct { ProductId = id });
        db.Promotions.Add(promo);
        await db.SaveChangesAsync();
    }

    // ---------------------------------------------------------------- khách hàng
    private record DemoAddress(string Province, string District, string Ward, string Street);
    private record DemoCustomer(ApplicationUser User, DemoAddress Address);

    private static readonly DemoAddress[] Addresses =
    {
        new("Thành phố Hà Nội", "Quận Cầu Giấy", "Phường Dịch Vọng", "Số 12 Trần Thái Tông"),
        new("Thành phố Hà Nội", "Quận Hà Đông", "Phường Mộ Lao", "Số 5 Tố Hữu"),
        new("Thành phố Hà Nội", "Quận Long Biên", "Phường Ngọc Lâm", "25 Nguyễn Văn Cừ"),
        new("Thành phố Hà Nội", "Quận Đống Đa", "Phường Láng Hạ", "88 Láng Hạ"),
        new("Thành phố Hà Nội", "Quận Thanh Xuân", "Phường Nhân Chính", "15 Lê Văn Lương"),
        new("Thành phố Hải Phòng", "Quận Lê Chân", "Phường An Biên", "102 Tô Hiệu"),
        new("Tỉnh Bắc Ninh", "Thành phố Bắc Ninh", "Phường Suối Hoa", "45 Lý Thái Tổ"),
        new("Thành phố Hồ Chí Minh", "Quận 3", "Phường Võ Thị Sáu", "210 Điện Biên Phủ"),
        new("Thành phố Đà Nẵng", "Quận Hải Châu", "Phường Hải Châu I", "60 Bạch Đằng"),
        new("Tỉnh Nam Định", "Thành phố Nam Định", "Phường Vị Xuyên", "18 Hùng Vương"),
    };

    private static readonly (string Name, string Email, Gender Gender)[] CustomerSeeds =
    {
        ("Nguyễn Thị Lan", "lan.nguyen", Gender.Female), ("Trần Thị Mai", "mai.tran", Gender.Female), ("Lê Thu Hà", "ha.le", Gender.Female),
        ("Phạm Thị Hương", "huong.pham", Gender.Female), ("Hoàng Minh Anh", "anh.hoang", Gender.Female), ("Vũ Thị Ngọc", "ngoc.vu", Gender.Female),
        ("Đặng Thùy Linh", "linh.dang", Gender.Female), ("Bùi Thị Thảo", "thao.bui", Gender.Female), ("Đỗ Quang Huy", "huy.do", Gender.Male),
        ("Ngô Thị Yến", "yen.ngo", Gender.Female), ("Dương Văn Nam", "nam.duong", Gender.Male), ("Lý Thị Hoa", "hoa.ly", Gender.Female),
        ("Phan Thanh Tùng", "tung.phan", Gender.Male), ("Trịnh Thị Nga", "nga.trinh", Gender.Female), ("Hồ Thị Kim", "kim.ho", Gender.Female),
    };

    private static readonly string[] GuestNames = { "Chị Hằng", "Anh Dũng", "Cô Liên", "Chị Phương", "Anh Tuấn", "Chị Trang", "Bác Hòa", "Chị Vân", "Anh Sơn", "Chị Quỳnh" };
    private static readonly string[] OrderNotes = { "Giao giờ hành chính", "Gọi trước khi giao", "Giao buổi tối sau 18h", "Để ở lễ tân tòa nhà", "Gói làm quà giúp em" };
    private static readonly string[] CustomerCancelReasons = { "Đặt nhầm size", "Muốn đổi màu khác", "Đặt trùng đơn", "Thay đổi địa chỉ nhận" };
    private static readonly string[] StaffCancelReasons = { "Không liên lạc được khách", "Khách báo hủy qua điện thoại", "Khách chưa chuyển khoản sau 24 giờ" };

    private static async Task<List<DemoCustomer>> SeedCustomersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext db, Random rnd, DateTime today)
    {
        var result = new List<DemoCustomer>();
        for (int i = 0; i < CustomerSeeds.Length; i++)
        {
            var (name, emailPrefix, gender) = CustomerSeeds[i];
            var email = $"{emailPrefix}@example.com";
            var address = Addresses[i % Addresses.Length];
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email, Email = email, EmailConfirmed = true, FullName = name, Gender = gender,
                    PhoneNumber = "09" + rnd.Next(10000000, 99999999),
                    DateOfBirth = new DateOnly(rnd.Next(1975, 2004), rnd.Next(1, 13), rnd.Next(1, 28)),
                    CreatedAt = today.AddDays(-rnd.Next(10, 89)).AddHours(rnd.Next(8, 22))
                };
                var created = await userManager.CreateAsync(user, DemoPassword);
                if (!created.Succeeded) throw new Exception("Không tạo được khách demo: " + string.Join("; ", created.Errors.Select(e => e.Description)));
                await userManager.AddToRoleAsync(user, SeedData.RoleCustomer);
                db.Addresses.Add(new Address
                {
                    UserId = user.Id, ReceiverName = name, Phone = user.PhoneNumber!, Province = address.Province,
                    District = address.District, Ward = address.Ward, Street = address.Street, IsDefault = true
                });
            }
            result.Add(new DemoCustomer(user, address));
        }
        await db.SaveChangesAsync();
        return result;
    }

    // ---------------------------------------------------------------- đánh giá, yêu thích, liên hệ
    private static readonly Dictionary<int, string[]> ReviewComments = new()
    {
        [5] = new[] { "Vải lanh mềm, mặc rất mát, đúng như mô tả.", "Form đẹp, đường may chắc chắn, sẽ ủng hộ tiếp.", "Giao hàng nhanh, đóng gói cẩn thận, mẹ mình rất thích.", "Màu ngoài đẹp hơn trong ảnh, giặt không ra màu.", "Mặc ở nhà thoải mái, giá hợp lý." },
        [4] = new[] { "Chất vải ổn, hơi rộng một chút so với size thường mặc.", "Hàng đẹp, giao hơi chậm một ngày.", "Vải mát, nên giặt tay để giữ form.", "Đúng mô tả, sẽ mua thêm màu khác." },
        [3] = new[] { "Vải được nhưng màu hơi nhạt hơn ảnh.", "Mặc ổn, đường chỉ ở gấu chưa thật đều.", "Size hơi nhỏ, nên chọn tăng một size." },
        [2] = new[] { "Vải mỏng hơn mình nghĩ.", "Giao nhầm màu, phải đổi lại mất thời gian." }
    };

    private static int SeedReviews(ApplicationDbContext db, List<Order> completedOrders, List<ProductVariant> variants, Random rnd, DateTime now)
    {
        int count = 0;
        foreach (var order in completedOrders.Where(o => o.UserId is not null))
        {
            foreach (var productId in order.Details.Select(d => variants.First(v => v.Id == d.VariantId).ProductId).Distinct())
            {
                if (rnd.NextDouble() > 0.55) continue;
                var roll = rnd.NextDouble();
                var rating = roll < 0.5 ? 5 : roll < 0.8 ? 4 : roll < 0.95 ? 3 : 2;
                var createdAt = Min(order.CompletedAt!.Value.AddDays(rnd.Next(1, 6)).AddHours(rnd.Next(0, 10)), now.AddMinutes(-5));
                db.Reviews.Add(new Review
                {
                    ProductId = productId, UserId = order.UserId!, OrderId = order.Id, Rating = (byte)rating,
                    Comment = ReviewComments[rating][rnd.Next(ReviewComments[rating].Length)],
                    IsApproved = createdAt < now.AddDays(-2) && rnd.NextDouble() < 0.9,   // đánh giá mới nhất còn chờ duyệt
                    CreatedAt = createdAt
                });
                count++;
            }
        }
        return count;
    }

    private static void SeedWishlists(ApplicationDbContext db, List<DemoCustomer> customers, List<ProductVariant> variants, Random rnd)
    {
        var productIds = variants.Select(v => v.ProductId).Distinct().ToList();
        foreach (var c in customers)
            foreach (var pid in productIds.OrderBy(_ => rnd.Next()).Take(rnd.Next(0, 4)))
                db.Wishlists.Add(new Wishlist { UserId = c.User.Id, ProductId = pid, CreatedAt = c.User.CreatedAt.AddDays(rnd.Next(0, 5)) });
    }

    private static void SeedContacts(ApplicationDbContext db, ApplicationUser staff, DateTime today)
    {
        db.Contacts.AddRange(
            new Contact { FullName = "Nguyễn Thu Trang", Phone = "0912345001", Email = "trang.nt@example.com", Subject = "Hỏi về size", Message = "Mình cao 1m58 nặng 52kg thì mặc bộ lanh nữ size M hay L ạ?", IsHandled = true, HandledByUserId = staff.Id, CreatedAt = today.AddDays(-20).AddHours(10) },
            new Contact { FullName = "Lê Văn Bình", Phone = "0912345002", Subject = "Mua sỉ", Message = "Cửa hàng có chính sách giá sỉ cho đơn từ 50 bộ không? Tôi muốn lấy hàng cho shop ở Thanh Hóa.", IsHandled = true, HandledByUserId = staff.Id, CreatedAt = today.AddDays(-9).AddHours(15) },
            new Contact { FullName = "Phạm Thị Hòa", Phone = "0912345003", Email = "hoa.pt@example.com", Subject = "Đổi hàng", Message = "Tôi nhận bộ lanh trung niên bị chật, muốn đổi sang size XXL thì làm thế nào?", CreatedAt = today.AddDays(-2).AddHours(9) },
            new Contact { FullName = "Trần Minh Khôi", Phone = "0912345004", Subject = "Hợp tác", Message = "Bên mình cung cấp vải lanh, muốn gửi báo giá cho cửa hàng.", CreatedAt = today.AddDays(-1).AddHours(14) });
    }

    // ---------------------------------------------------------------- tiện ích
    private static string NextCode(string prefix, DateTime date, Dictionary<string, int> seq)
    {
        var key = prefix + date.ToString("yyMMdd");
        seq[key] = seq.GetValueOrDefault(key) + 1;
        return key + seq[key].ToString("000");
    }

    private static T Pick<T>(List<T> list, Random rnd) => list[rnd.Next(list.Count)];
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
