using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Services;

/// <summary>Giá gốc và giá sau khuyến mãi của một sản phẩm/biến thể.</summary>
public record PriceInfo(decimal OriginalPrice, decimal SalePrice)
{
    public bool HasDiscount => SalePrice < OriginalPrice;
    public int DiscountPercent => OriginalPrice <= 0 ? 0 : (int)Math.Round((1 - SalePrice / OriginalPrice) * 100);
}

/// <summary>Tính giá khuyến mãi theo các chương trình Promotion đang hiệu lực (quy tắc 4.5 trong tài liệu CSDL).</summary>
public interface IPricingService
{
    Task<Dictionary<int, List<Promotion>>> GetActivePromotionsAsync(IEnumerable<int> productIds);
    PriceInfo Calculate(decimal basePrice, IEnumerable<Promotion>? promotions);
    Task<List<int>> GetProductIdsOnSaleAsync();
}

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _db;
    public PricingService(ApplicationDbContext db) => _db = db;

    public async Task<Dictionary<int, List<Promotion>>> GetActivePromotionsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, List<Promotion>>();
        var now = DateTime.Now;
        var rows = await _db.PromotionProducts.AsNoTracking()
            .Where(pp => ids.Contains(pp.ProductId)
                         && pp.Promotion.IsActive && pp.Promotion.StartDate <= now && pp.Promotion.EndDate >= now)
            .Select(pp => new { pp.ProductId, pp.Promotion })
            .ToListAsync();
        return rows.GroupBy(r => r.ProductId).ToDictionary(g => g.Key, g => g.Select(x => x.Promotion).ToList());
    }

    public PriceInfo Calculate(decimal basePrice, IEnumerable<Promotion>? promotions)
    {
        var best = basePrice;
        if (promotions is not null)
        {
            foreach (var p in promotions)
            {
                var sale = p.DiscountType == DiscountType.Percent
                    ? basePrice * (1 - p.DiscountValue / 100m)
                    : basePrice - p.DiscountValue;
                sale = Math.Max(0, Math.Round(sale, 0));
                if (sale < best) best = sale;
            }
        }
        return new PriceInfo(basePrice, best);
    }

    public async Task<List<int>> GetProductIdsOnSaleAsync()
    {
        var now = DateTime.Now;
        return await _db.PromotionProducts.AsNoTracking()
            .Where(pp => pp.Promotion.IsActive && pp.Promotion.StartDate <= now && pp.Promotion.EndDate >= now)
            .Select(pp => pp.ProductId).Distinct().ToListAsync();
    }
}
