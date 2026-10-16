using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Tests;

/// <summary>Kiểm thử điều kiện và cách tính mã giảm giá (KH06).</summary>
public class PromotionServiceTests
{
    private static Coupon Vt10(Action<Coupon>? customize = null)
    {
        var c = new Coupon
        {
            Code = "VT10", DiscountType = DiscountType.Percent, DiscountValue = 10, MaxDiscountAmount = 50000,
            MinOrderAmount = 300000, UsageLimit = 100, UsedCount = 0,
            StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(30), IsActive = true
        };
        customize?.Invoke(c);
        return c;
    }

    private static async Task<(PromotionService Service, ApplicationDbContext Db)> CreateAsync(params Coupon[] coupons)
    {
        var db = TestDb.Create();
        db.Coupons.AddRange(coupons);
        await db.SaveChangesAsync();
        return (new PromotionService(db), db);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_KhongNhapMa_KhongLoiKhongGiam(string? code)
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync(code, 500000);

        Assert.Null(result.Coupon);
        Assert.Null(result.Error);
        Assert.Equal(0, result.Discount);
    }

    [Fact]
    public async Task Validate_MaKhongTonTai_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync("KHONGCO", 500000);

        Assert.False(result.Success);
        Assert.Contains("không tồn tại", result.Error);
    }

    [Fact]
    public async Task Validate_MaDaTat_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10(c => c.IsActive = false));

        var result = await service.ValidateCouponAsync("VT10", 500000);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validate_ChuaDenNgay_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10(c => c.StartDate = DateTime.Now.AddDays(2)));

        var result = await service.ValidateCouponAsync("VT10", 500000);

        Assert.Contains("chưa đến", result.Error);
    }

    [Fact]
    public async Task Validate_HetHan_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10(c => { c.StartDate = DateTime.Now.AddDays(-10); c.EndDate = DateTime.Now.AddDays(-1); }));

        var result = await service.ValidateCouponAsync("VT10", 500000);

        Assert.Contains("hết hạn", result.Error);
    }

    [Fact]
    public async Task Validate_HetLuotSuDung_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10(c => { c.UsageLimit = 5; c.UsedCount = 5; }));

        var result = await service.ValidateCouponAsync("VT10", 500000);

        Assert.Contains("hết lượt", result.Error);
    }

    [Fact]
    public async Task Validate_KhongGioiHanLuot_VanDungDuoc()
    {
        var (service, _) = await CreateAsync(Vt10(c => { c.UsageLimit = null; c.UsedCount = 9999; }));

        var result = await service.ValidateCouponAsync("VT10", 500000);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Validate_DonChuaDatToiThieu_BaoLoi()
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync("VT10", 299000);

        Assert.False(result.Success);
        Assert.Contains("300", result.Error);
    }

    [Fact]
    public async Task Validate_GiamPhanTram_DuoiMucToiDa()
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync("VT10", 300000);

        Assert.True(result.Success);
        Assert.Equal(30000, result.Discount);   // 10% của 300.000
    }

    [Fact]
    public async Task Validate_GiamPhanTram_BiChanBoiMucToiDa()
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync("VT10", 572000);

        Assert.Equal(50000, result.Discount);   // 10% = 57.200 nhưng tối đa 50.000
    }

    [Fact]
    public async Task Validate_MaGiamSoTien_GiamDungGiaTri()
    {
        var giam30K = new Coupon { Code = "GIAM30K", DiscountType = DiscountType.Amount, DiscountValue = 30000, MinOrderAmount = 200000, StartDate = DateTime.Now.AddDays(-1), EndDate = DateTime.Now.AddDays(10), IsActive = true };
        var (service, _) = await CreateAsync(giam30K);

        var result = await service.ValidateCouponAsync("GIAM30K", 250000);

        Assert.True(result.Success);
        Assert.Equal(30000, result.Discount);
    }

    [Theory]
    [InlineData("vt10")]
    [InlineData("  VT10  ")]
    [InlineData("Vt10")]
    public async Task Validate_KhongPhanBietHoaThuongVaKhoangTrang(string input)
    {
        var (service, _) = await CreateAsync(Vt10());

        var result = await service.ValidateCouponAsync(input, 400000);

        Assert.True(result.Success);
        Assert.Equal("VT10", result.Coupon!.Code);
    }

    [Fact]
    public void CalculateDiscount_KhongVuotQuaTienHang()
    {
        var service = new PromotionService(TestDb.Create());
        var coupon = new Coupon { DiscountType = DiscountType.Amount, DiscountValue = 100000 };

        var discount = service.CalculateCouponDiscount(coupon, 60000);

        Assert.Equal(60000, discount);
    }
}
