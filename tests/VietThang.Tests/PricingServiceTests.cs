using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Tests;

/// <summary>Kiểm thử quy tắc tính giá khuyến mãi (mục 4.5 tài liệu thiết kế CSDL).</summary>
public class PricingServiceTests
{
    private static Promotion Percent(decimal value) => new() { DiscountType = DiscountType.Percent, DiscountValue = value, IsActive = true };
    private static Promotion Amount(decimal value) => new() { DiscountType = DiscountType.Amount, DiscountValue = value, IsActive = true };

    private static PricingService CreateService() => new(TestDb.Create());

    [Fact]
    public void Calculate_KhongCoKhuyenMai_GiuNguyenGia()
    {
        var price = CreateService().Calculate(305000, null);

        Assert.Equal(305000, price.OriginalPrice);
        Assert.Equal(305000, price.SalePrice);
        Assert.False(price.HasDiscount);
        Assert.Equal(0, price.DiscountPercent);
    }

    [Theory]
    [InlineData(305000, 30, 213500)]   // ví dụ thực tế: bộ lanh 305.000 đ giảm 30%
    [InlineData(275000, 30, 192500)]
    [InlineData(415000, 10, 373500)]
    [InlineData(200000, 100, 0)]
    public void Calculate_GiamPhanTram_TinhDung(decimal basePrice, decimal percent, decimal expected)
    {
        var price = CreateService().Calculate(basePrice, new[] { Percent(percent) });

        Assert.Equal(expected, price.SalePrice);
        Assert.True(price.HasDiscount);
    }

    [Fact]
    public void Calculate_GiamSoTien_TruThangVaoGia()
    {
        var price = CreateService().Calculate(250000, new[] { Amount(40000) });

        Assert.Equal(210000, price.SalePrice);
        Assert.Equal(16, price.DiscountPercent);
    }

    [Fact]
    public void Calculate_NhieuChuongTrinh_LayMucGiamLonNhat()
    {
        var price = CreateService().Calculate(300000, new[] { Percent(10), Amount(50000), Percent(20) });

        // 10% → 270.000 ; 50.000 đ → 250.000 ; 20% → 240.000  ⇒ chọn 240.000
        Assert.Equal(240000, price.SalePrice);
    }

    [Fact]
    public void Calculate_GiamNhieuHonGia_KhongAm()
    {
        var price = CreateService().Calculate(100000, new[] { Amount(150000) });

        Assert.Equal(0, price.SalePrice);
    }

    [Fact]
    public void Calculate_DanhSachRong_KhongGiam()
    {
        var price = CreateService().Calculate(180000, Array.Empty<Promotion>());

        Assert.Equal(180000, price.SalePrice);
    }

    [Fact]
    public async Task GetActivePromotions_ChiLayChuongTrinhDangHieuLuc()
    {
        using var db = TestDb.Create();
        var now = DateTime.Now;
        var product = new Product { Code = "P1", Name = "SP 1", Slug = "sp-1", Price = 100000 };
        db.Products.Add(product);
        db.Promotions.AddRange(
            Promo("Đang chạy", now.AddDays(-1), now.AddDays(5), true, product),
            Promo("Hết hạn", now.AddDays(-10), now.AddDays(-1), true, product),
            Promo("Chưa bắt đầu", now.AddDays(2), now.AddDays(9), true, product),
            Promo("Đã tắt", now.AddDays(-1), now.AddDays(5), false, product));
        await db.SaveChangesAsync();

        var result = await new PricingService(db).GetActivePromotionsAsync(new[] { product.Id });

        var promos = Assert.Single(result).Value;
        Assert.Equal("Đang chạy", Assert.Single(promos).Name);
    }

    [Fact]
    public async Task GetProductIdsOnSale_KhongTrungLapVaBoQuaHetHan()
    {
        using var db = TestDb.Create();
        var now = DateTime.Now;
        var onSale = new Product { Code = "A", Name = "A", Slug = "a", Price = 1 };
        var expired = new Product { Code = "B", Name = "B", Slug = "b", Price = 1 };
        db.Products.AddRange(onSale, expired);
        db.Promotions.AddRange(
            Promo("KM 1", now.AddDays(-1), now.AddDays(1), true, onSale),
            Promo("KM 2", now.AddDays(-2), now.AddDays(2), true, onSale),
            Promo("Cũ", now.AddDays(-9), now.AddDays(-3), true, expired));
        await db.SaveChangesAsync();

        var ids = await new PricingService(db).GetProductIdsOnSaleAsync();

        Assert.Equal(new[] { onSale.Id }, ids);
    }

    private static Promotion Promo(string name, DateTime start, DateTime end, bool active, Product product)
    {
        var promo = new Promotion { Name = name, DiscountType = DiscountType.Percent, DiscountValue = 10, StartDate = start, EndDate = end, IsActive = active };
        promo.PromotionProducts.Add(new PromotionProduct { Product = product });
        return promo;
    }
}
