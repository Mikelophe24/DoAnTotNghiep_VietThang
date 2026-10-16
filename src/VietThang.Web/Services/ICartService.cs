using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;

namespace VietThang.Web.Services;

public class CartLine
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal UnitPrice { get; set; }
    /// <summary>Giá vốn hiện tại của biến thể, dùng để lưu snapshot vào chi tiết đơn. Không trả về cho client.</summary>
    [JsonIgnore] public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public bool IsAvailable => IsActive && Quantity > 0 && Stock >= Quantity;
}

public class CartSummary
{
    public List<CartLine> Lines { get; set; } = new();
    public decimal SubTotal => Lines.Sum(l => l.LineTotal);
    public int TotalQuantity => Lines.Sum(l => l.Quantity);
    public string? CouponCode { get; set; }
    public Coupon? Coupon { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponError { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal FreeShippingThreshold { get; set; }
    public decimal Total => Math.Max(0, SubTotal + ShippingFee - DiscountAmount);
    public bool IsEmpty => Lines.Count == 0;
}

/// <summary>
/// Giỏ hàng (KH05). Khách vãng lai giữ giỏ ở trình duyệt (Angular localStorage) và gọi BuildLines/BuildSummary để tính giá;
/// khách đăng nhập (JWT) có giỏ lưu trong CSDL qua các phương thức Add/Update/Remove.
/// </summary>
public interface ICartService
{
    /// <summary>Các dòng trong giỏ CSDL của người dùng hiện tại (rỗng nếu chưa đăng nhập).</summary>
    Task<List<CartLine>> GetLinesAsync();
    /// <summary>Dựng dòng giỏ từ danh sách biến thể bất kỳ (giỏ khách vãng lai gửi từ Angular).</summary>
    Task<List<CartLine>> BuildLinesAsync(IEnumerable<(int VariantId, int Quantity)> items);
    /// <summary>Tính tổng, phí ship, mã giảm giá cho các dòng đã dựng.</summary>
    Task<CartSummary> BuildSummaryAsync(List<CartLine> lines, string? couponCode);
    Task<(bool Ok, string Message)> AddAsync(int variantId, int quantity);
    Task UpdateAsync(int variantId, int quantity);
    Task RemoveAsync(int variantId);
    Task ClearAsync();
    Task<int> CountAsync();
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IPricingService _pricing;
    private readonly IPromotionService _promotions;
    private readonly ISettingService _settings;

    public CartService(ApplicationDbContext db, IHttpContextAccessor http, IPricingService pricing,
        IPromotionService promotions, ISettingService settings)
    {
        _db = db;
        _http = http;
        _pricing = pricing;
        _promotions = promotions;
        _settings = settings;
    }

    private string? UserId
    {
        get
        {
            var user = _http.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        }
    }

    private string RequireUserId() => UserId ?? throw new InvalidOperationException("Giỏ hàng trong CSDL chỉ dùng cho khách đã đăng nhập.");

    private async Task<Cart> GetOrCreateDbCartAsync(string userId)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
        }
        return cart;
    }

    public async Task<List<CartLine>> GetLinesAsync()
    {
        if (UserId is not { } uid) return new List<CartLine>();
        var raw = await _db.CartItems.AsNoTracking().Where(i => i.Cart.UserId == uid)
            .Select(i => new ValueTuple<int, int>(i.VariantId, i.Quantity)).ToListAsync();
        return await BuildLinesAsync(raw);
    }

    public async Task<List<CartLine>> BuildLinesAsync(IEnumerable<(int VariantId, int Quantity)> items)
    {
        var raw = items.Where(i => i.Quantity > 0).GroupBy(i => i.VariantId)
            .Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();
        if (raw.Count == 0) return new List<CartLine>();
        var ids = raw.Select(r => r.VariantId).ToList();

        var variants = await _db.ProductVariants.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new
            {
                v.Id, v.ProductId, v.Sku, v.StockQuantity, v.Price, v.CostPrice,
                IsActive = v.IsActive && v.Product.IsActive,
                ProductName = v.Product.Name, v.Product.Slug, BasePrice = v.Product.Price,
                ColorName = v.Color.Name, SizeName = v.Size.Name,
                ImageUrl = v.Product.Images.Where(i => i.ColorId == v.ColorId).OrderByDescending(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                        ?? v.Product.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault()
            })
            .ToListAsync();

        var promos = await _pricing.GetActivePromotionsAsync(variants.Select(v => v.ProductId));
        var lines = new List<CartLine>();
        foreach (var r in raw)
        {
            var v = variants.FirstOrDefault(x => x.Id == r.VariantId);
            if (v is null) continue;
            var price = _pricing.Calculate(v.Price ?? v.BasePrice, promos.GetValueOrDefault(v.ProductId));
            lines.Add(new CartLine
            {
                VariantId = v.Id, ProductId = v.ProductId, ProductName = v.ProductName, Slug = v.Slug, ImageUrl = v.ImageUrl,
                ColorName = v.ColorName, SizeName = v.SizeName, Sku = v.Sku,
                OriginalPrice = price.OriginalPrice, UnitPrice = price.SalePrice, CostPrice = v.CostPrice,
                Quantity = r.Quantity, Stock = v.StockQuantity, IsActive = v.IsActive
            });
        }
        return lines;
    }

    public async Task<CartSummary> BuildSummaryAsync(List<CartLine> lines, string? couponCode)
    {
        var summary = new CartSummary
        {
            Lines = lines,
            CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim().ToUpperInvariant(),
            FreeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000)
        };
        if (summary.Lines.Count == 0) return summary;

        summary.ShippingFee = summary.SubTotal >= summary.FreeShippingThreshold
            ? 0
            : await _settings.GetDecimalAsync(SettingKeys.DefaultShippingFee, 30000);

        if (!string.IsNullOrEmpty(summary.CouponCode))
        {
            var result = await _promotions.ValidateCouponAsync(summary.CouponCode, summary.SubTotal);
            summary.Coupon = result.Coupon;
            summary.DiscountAmount = result.Discount;
            summary.CouponError = result.Error;
        }
        return summary;
    }

    public async Task<(bool Ok, string Message)> AddAsync(int variantId, int quantity)
    {
        var uid = RequireUserId();
        if (quantity < 1) quantity = 1;
        var variant = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new { v.StockQuantity, IsActive = v.IsActive && v.Product.IsActive, v.Product.Name })
            .FirstOrDefaultAsync();
        if (variant is null || !variant.IsActive) return (false, "Sản phẩm không còn kinh doanh.");
        if (variant.StockQuantity <= 0) return (false, "Sản phẩm đã hết hàng.");

        var cart = await GetOrCreateDbCartAsync(uid);
        var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
        var newQty = (item?.Quantity ?? 0) + quantity;
        if (newQty > variant.StockQuantity) return (false, $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho.");
        if (item is null) cart.Items.Add(new CartItem { VariantId = variantId, Quantity = newQty });
        else item.Quantity = newQty;
        cart.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return (true, $"Đã thêm \"{variant.Name}\" vào giỏ hàng.");
    }

    public async Task UpdateAsync(int variantId, int quantity)
    {
        var uid = RequireUserId();
        if (quantity <= 0) { await RemoveAsync(variantId); return; }
        var stock = await _db.ProductVariants.Where(v => v.Id == variantId).Select(v => v.StockQuantity).FirstOrDefaultAsync();
        quantity = Math.Min(quantity, Math.Max(stock, 1));
        var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Cart.UserId == uid && i.VariantId == variantId);
        if (item is not null) { item.Quantity = quantity; await _db.SaveChangesAsync(); }
    }

    public async Task RemoveAsync(int variantId)
    {
        var uid = RequireUserId();
        await _db.CartItems.Where(i => i.Cart.UserId == uid && i.VariantId == variantId).ExecuteDeleteAsync();
    }

    public async Task ClearAsync()
    {
        var uid = RequireUserId();
        await _db.CartItems.Where(i => i.Cart.UserId == uid).ExecuteDeleteAsync();
    }

    public async Task<int> CountAsync()
    {
        if (UserId is not { } uid) return 0;
        return await _db.CartItems.Where(i => i.Cart.UserId == uid).SumAsync(i => (int?)i.Quantity) ?? 0;
    }
}
