using System.Security.Claims;
using System.Text.Json;
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

/// <summary>Giỏ hàng (KH05): khách vãng lai lưu Session, khách đăng nhập lưu CSDL.</summary>
public interface ICartService
{
    Task<List<CartLine>> GetLinesAsync();
    Task<CartSummary> GetSummaryAsync();
    /// <summary>Dựng dòng giỏ từ danh sách biến thể bất kỳ (giỏ khách vãng lai gửi từ Angular).</summary>
    Task<List<CartLine>> BuildLinesAsync(IEnumerable<(int VariantId, int Quantity)> items);
    /// <summary>Tính tổng, phí ship, mã giảm giá cho các dòng đã dựng.</summary>
    Task<CartSummary> BuildSummaryAsync(List<CartLine> lines, string? couponCode);
    Task<(bool Ok, string Message)> AddAsync(int variantId, int quantity);
    Task UpdateAsync(int variantId, int quantity);
    Task RemoveAsync(int variantId);
    Task ClearAsync();
    Task<int> CountAsync();
    Task MergeSessionCartToUserAsync(string userId);
    string? GetCouponCode();
    void SetCouponCode(string? code);
}

public class CartService : ICartService
{
    private const string SessionKey = "Cart";
    private const string CouponKey = "CouponCode";
    private record SessionItem(int VariantId, int Quantity);

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

    private HttpContext Ctx => _http.HttpContext ?? throw new InvalidOperationException("Không có HttpContext.");
    private string? UserId => Ctx.User.Identity?.IsAuthenticated == true ? Ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    // ---------- Session ----------
    private List<SessionItem> ReadSession()
    {
        var json = Ctx.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json) ? new List<SessionItem>() : JsonSerializer.Deserialize<List<SessionItem>>(json) ?? new List<SessionItem>();
    }
    private void WriteSession(List<SessionItem> items) => Ctx.Session.SetString(SessionKey, JsonSerializer.Serialize(items));

    // ---------- DB ----------
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

    private async Task<List<(int VariantId, int Quantity)>> GetRawItemsAsync()
    {
        if (UserId is { } uid)
        {
            return await _db.CartItems.AsNoTracking().Where(i => i.Cart.UserId == uid)
                .Select(i => new ValueTuple<int, int>(i.VariantId, i.Quantity)).ToListAsync();
        }
        return ReadSession().Select(s => (s.VariantId, s.Quantity)).ToList();
    }

    public async Task<List<CartLine>> GetLinesAsync() => await BuildLinesAsync(await GetRawItemsAsync());

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
                v.Id, v.ProductId, v.Sku, v.StockQuantity, v.Price,
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
            var basePrice = v.Price ?? v.BasePrice;
            var price = _pricing.Calculate(basePrice, promos.GetValueOrDefault(v.ProductId));
            lines.Add(new CartLine
            {
                VariantId = v.Id, ProductId = v.ProductId, ProductName = v.ProductName, Slug = v.Slug, ImageUrl = v.ImageUrl,
                ColorName = v.ColorName, SizeName = v.SizeName, Sku = v.Sku,
                OriginalPrice = price.OriginalPrice, UnitPrice = price.SalePrice,
                Quantity = r.Quantity, Stock = v.StockQuantity, IsActive = v.IsActive
            });
        }
        return lines;
    }

    public async Task<CartSummary> GetSummaryAsync() => await BuildSummaryAsync(await GetLinesAsync(), GetCouponCode());

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
        if (quantity < 1) quantity = 1;
        var variant = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new { v.StockQuantity, IsActive = v.IsActive && v.Product.IsActive, v.Product.Name })
            .FirstOrDefaultAsync();
        if (variant is null || !variant.IsActive) return (false, "Sản phẩm không còn kinh doanh.");
        if (variant.StockQuantity <= 0) return (false, "Sản phẩm đã hết hàng.");

        if (UserId is { } uid)
        {
            var cart = await GetOrCreateDbCartAsync(uid);
            var item = cart.Items.FirstOrDefault(i => i.VariantId == variantId);
            var newQty = (item?.Quantity ?? 0) + quantity;
            if (newQty > variant.StockQuantity) return (false, $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho.");
            if (item is null) cart.Items.Add(new CartItem { VariantId = variantId, Quantity = newQty });
            else item.Quantity = newQty;
            cart.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }
        else
        {
            var items = ReadSession();
            var idx = items.FindIndex(i => i.VariantId == variantId);
            var newQty = (idx >= 0 ? items[idx].Quantity : 0) + quantity;
            if (newQty > variant.StockQuantity) return (false, $"Chỉ còn {variant.StockQuantity} sản phẩm trong kho.");
            if (idx >= 0) items[idx] = new SessionItem(variantId, newQty);
            else items.Add(new SessionItem(variantId, newQty));
            WriteSession(items);
        }
        return (true, $"Đã thêm \"{variant.Name}\" vào giỏ hàng.");
    }

    public async Task UpdateAsync(int variantId, int quantity)
    {
        if (quantity <= 0) { await RemoveAsync(variantId); return; }
        var stock = await _db.ProductVariants.Where(v => v.Id == variantId).Select(v => v.StockQuantity).FirstOrDefaultAsync();
        quantity = Math.Min(quantity, Math.Max(stock, 1));

        if (UserId is { } uid)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(i => i.Cart.UserId == uid && i.VariantId == variantId);
            if (item is not null) { item.Quantity = quantity; await _db.SaveChangesAsync(); }
        }
        else
        {
            var items = ReadSession();
            var idx = items.FindIndex(i => i.VariantId == variantId);
            if (idx >= 0) { items[idx] = new SessionItem(variantId, quantity); WriteSession(items); }
        }
    }

    public async Task RemoveAsync(int variantId)
    {
        if (UserId is { } uid)
        {
            await _db.CartItems.Where(i => i.Cart.UserId == uid && i.VariantId == variantId).ExecuteDeleteAsync();
        }
        else
        {
            var items = ReadSession();
            items.RemoveAll(i => i.VariantId == variantId);
            WriteSession(items);
        }
    }

    public async Task ClearAsync()
    {
        if (UserId is { } uid)
            await _db.CartItems.Where(i => i.Cart.UserId == uid).ExecuteDeleteAsync();
        else
            Ctx.Session.Remove(SessionKey);
    }

    public async Task<int> CountAsync() => (await GetRawItemsAsync()).Sum(r => r.Quantity);

    public async Task MergeSessionCartToUserAsync(string userId)
    {
        var items = ReadSession();
        if (items.Count == 0) return;
        var cart = await GetOrCreateDbCartAsync(userId);
        foreach (var s in items)
        {
            var existing = cart.Items.FirstOrDefault(i => i.VariantId == s.VariantId);
            if (existing is null) cart.Items.Add(new CartItem { VariantId = s.VariantId, Quantity = s.Quantity });
            else existing.Quantity += s.Quantity;
        }
        cart.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        Ctx.Session.Remove(SessionKey);
    }

    public string? GetCouponCode() => Ctx.Session.GetString(CouponKey);

    public void SetCouponCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) Ctx.Session.Remove(CouponKey);
        else Ctx.Session.SetString(CouponKey, code.Trim().ToUpperInvariant());
    }
}
