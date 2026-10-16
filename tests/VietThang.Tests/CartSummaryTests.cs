using Microsoft.AspNetCore.Http;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Tests;

/// <summary>Kiểm thử tổng tiền giỏ hàng: phí vận chuyển, ngưỡng freeship, mã giảm giá, tình trạng còn hàng.</summary>
public class CartSummaryTests
{
    private static CartService CreateService(ApplicationDbContext db)
        => new(db, new HttpContextAccessor(), new PricingService(db), new PromotionService(db), TestDb.Settings(db));

    private static CartLine Line(decimal unitPrice, int quantity = 1, int stock = 10, bool active = true)
        => new() { VariantId = 1, ProductName = "Bộ lanh", UnitPrice = unitPrice, OriginalPrice = unitPrice, Quantity = quantity, Stock = stock, IsActive = active };

    [Fact]
    public async Task DuoiNguongFreeship_TinhPhiVanChuyen()
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db);

        var summary = await CreateService(db).BuildSummaryAsync(new List<CartLine> { Line(213500, 2) }, null);

        Assert.Equal(427000, summary.SubTotal);
        Assert.Equal(30000, summary.ShippingFee);
        Assert.Equal(457000, summary.Total);
    }

    [Theory]
    [InlineData(500000)]   // đúng bằng ngưỡng
    [InlineData(572000)]
    public async Task DatNguongFreeship_MienPhiVanChuyen(decimal subTotal)
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db);

        var summary = await CreateService(db).BuildSummaryAsync(new List<CartLine> { Line(subTotal) }, null);

        Assert.Equal(0, summary.ShippingFee);
        Assert.Equal(subTotal, summary.Total);
    }

    [Fact]
    public async Task ThayDoiCauHinh_ApDungPhiVaNguongMoi()
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db, fee: 25000, threshold: 300000);
        var service = CreateService(db);

        var below = await service.BuildSummaryAsync(new List<CartLine> { Line(299000) }, null);
        var above = await service.BuildSummaryAsync(new List<CartLine> { Line(300000) }, null);

        Assert.Equal(25000, below.ShippingFee);
        Assert.Equal(0, above.ShippingFee);
    }

    [Fact]
    public async Task CoMaGiamGiaHopLe_TruVaoTongTien()
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db);
        db.Coupons.Add(new Coupon { Code = "VT10", DiscountType = DiscountType.Percent, DiscountValue = 10, MaxDiscountAmount = 50000, MinOrderAmount = 300000, StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(5), IsActive = true });
        await db.SaveChangesAsync();

        // Đơn thử thực tế: 2 bộ lanh 213.500 + 1 áo 145.000 = 572.000 → freeship, giảm tối đa 50.000
        var lines = new List<CartLine> { Line(213500, 2), Line(145000) };
        var summary = await CreateService(db).BuildSummaryAsync(lines, " vt10 ");

        Assert.Equal(572000, summary.SubTotal);
        Assert.Equal(0, summary.ShippingFee);
        Assert.Equal(50000, summary.DiscountAmount);
        Assert.Equal(522000, summary.Total);
        Assert.Equal("VT10", summary.Coupon!.Code);
        Assert.Null(summary.CouponError);
    }

    [Fact]
    public async Task MaGiamGiaKhongHopLe_KhongGiamVaCoThongBao()
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db);

        var summary = await CreateService(db).BuildSummaryAsync(new List<CartLine> { Line(400000) }, "SAIMA");

        Assert.Null(summary.Coupon);
        Assert.Equal(0, summary.DiscountAmount);
        Assert.NotNull(summary.CouponError);
        Assert.Equal(430000, summary.Total);
    }

    [Fact]
    public async Task GioRong_KhongTinhPhiShip()
    {
        using var db = TestDb.Create();
        TestDb.AddShippingSettings(db);

        var summary = await CreateService(db).BuildSummaryAsync(new List<CartLine>(), "VT10");

        Assert.True(summary.IsEmpty);
        Assert.Equal(0, summary.ShippingFee);
        Assert.Equal(0, summary.Total);
    }

    [Theory]
    [InlineData(2, 5, true, true)]     // đủ hàng
    [InlineData(5, 5, true, true)]     // mua đúng bằng tồn
    [InlineData(6, 5, true, false)]    // vượt tồn
    [InlineData(1, 0, true, false)]    // hết hàng
    [InlineData(1, 5, false, false)]   // sản phẩm ngừng bán
    public void CartLine_IsAvailable_TheoTonKhoVaTrangThai(int quantity, int stock, bool active, bool expected)
        => Assert.Equal(expected, Line(100000, quantity, stock, active).IsAvailable);

    [Fact]
    public void CartSummary_TongKhongBaoGioAm()
    {
        var summary = new CartSummary { Lines = { Line(20000) }, DiscountAmount = 50000 };

        Assert.Equal(0, summary.Total);
    }
}
