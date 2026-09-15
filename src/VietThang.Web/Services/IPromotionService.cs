using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Services;

public record CouponResult(Coupon? Coupon, decimal Discount, string? Error)
{
    public bool Success => Coupon is not null && Error is null;
}

/// <summary>Kiểm tra và tính mã giảm giá (KH06).</summary>
public interface IPromotionService
{
    Task<CouponResult> ValidateCouponAsync(string? code, decimal subTotal);
    decimal CalculateCouponDiscount(Coupon coupon, decimal subTotal);
}

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _db;
    public PromotionService(ApplicationDbContext db) => _db = db;

    public async Task<CouponResult> ValidateCouponAsync(string? code, decimal subTotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return new CouponResult(null, 0, null);
        code = code.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code);
        if (coupon is null || !coupon.IsActive) return new CouponResult(null, 0, "Mã giảm giá không tồn tại.");
        var now = DateTime.Now;
        if (now < coupon.StartDate) return new CouponResult(null, 0, "Mã giảm giá chưa đến thời gian áp dụng.");
        if (now > coupon.EndDate) return new CouponResult(null, 0, "Mã giảm giá đã hết hạn.");
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
            return new CouponResult(null, 0, "Mã giảm giá đã hết lượt sử dụng.");
        if (subTotal < coupon.MinOrderAmount)
            return new CouponResult(null, 0, $"Đơn hàng phải từ {coupon.MinOrderAmount:N0} đ để dùng mã này.");
        return new CouponResult(coupon, CalculateCouponDiscount(coupon, subTotal), null);
    }

    public decimal CalculateCouponDiscount(Coupon coupon, decimal subTotal)
    {
        decimal discount = coupon.DiscountType == DiscountType.Percent
            ? Math.Round(subTotal * coupon.DiscountValue / 100m, 0)
            : coupon.DiscountValue;
        if (coupon.DiscountType == DiscountType.Percent && coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
        return Math.Min(discount, subTotal);
    }
}
