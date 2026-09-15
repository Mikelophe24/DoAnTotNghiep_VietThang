using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Services;

/// <summary>Truy vấn sản phẩm cho storefront: lọc, sắp xếp, chuyển thành thẻ sản phẩm có giá khuyến mãi.</summary>
public interface ICatalogService
{
    IQueryable<Product> ActiveProducts();
    IQueryable<Product> ApplyFilter(IQueryable<Product> query, ProductFilter filter);
    IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort);
    Task<List<ProductCardVm>> ToCardsAsync(IQueryable<Product> query, int take);
    Task<PagedList<ProductCardVm>> ToPagedCardsAsync(IQueryable<Product> query, int page, int pageSize);
    Task<List<int>> CategoryTreeIdsAsync(int categoryId);
}

public class CatalogService : ICatalogService
{
    private readonly ApplicationDbContext _db;
    private readonly IPricingService _pricing;

    public CatalogService(ApplicationDbContext db, IPricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    public IQueryable<Product> ActiveProducts() => _db.Products.AsNoTracking().Where(p => p.IsActive && p.Category.IsActive);

    public IQueryable<Product> ApplyFilter(IQueryable<Product> query, ProductFilter f)
    {
        if (!string.IsNullOrWhiteSpace(f.Q))
        {
            var q = f.Q.Trim();
            query = query.Where(p => p.Name.Contains(q) || p.Code.Contains(q) || (p.Material != null && p.Material.Contains(q)));
        }
        if (f.ColorIds.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && f.ColorIds.Contains(v.ColorId)));
        if (f.SizeIds.Count > 0)
            query = query.Where(p => p.Variants.Any(v => v.IsActive && f.SizeIds.Contains(v.SizeId)));
        if (f.MinPrice.HasValue) query = query.Where(p => p.Price >= f.MinPrice.Value);
        if (f.MaxPrice.HasValue) query = query.Where(p => p.Price <= f.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(f.Material)) query = query.Where(p => p.Material == f.Material);
        return query;
    }

    public IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort) => sort switch
    {
        "price-asc" => query.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
        "price-desc" => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
        "bestseller" => query.OrderByDescending(p => p.SoldCount).ThenByDescending(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.CreatedAt)
    };

    private IQueryable<ProductCardVm> Project(IQueryable<Product> query) => query.Select(p => new ProductCardVm
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug,
        CategoryName = p.Category.Name,
        ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                   ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
        OriginalPrice = p.Price,
        SalePrice = p.Price,
        IsNew = p.IsNew,
        InStock = p.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
        SoldCount = p.SoldCount
    });

    private async Task ApplyPricesAsync(List<ProductCardVm> cards)
    {
        var promos = await _pricing.GetActivePromotionsAsync(cards.Select(c => c.Id));
        foreach (var c in cards)
        {
            var price = _pricing.Calculate(c.OriginalPrice, promos.GetValueOrDefault(c.Id));
            c.SalePrice = price.SalePrice;
            c.DiscountPercent = price.DiscountPercent;
        }
    }

    public async Task<List<ProductCardVm>> ToCardsAsync(IQueryable<Product> query, int take)
    {
        var cards = await Project(query).Take(take).ToListAsync();
        await ApplyPricesAsync(cards);
        return cards;
    }

    public async Task<PagedList<ProductCardVm>> ToPagedCardsAsync(IQueryable<Product> query, int page, int pageSize)
    {
        var paged = await PagedList<ProductCardVm>.CreateAsync(Project(query), page, pageSize);
        await ApplyPricesAsync(paged.Items);
        return paged;
    }

    public async Task<List<int>> CategoryTreeIdsAsync(int categoryId)
    {
        var children = await _db.Categories.Where(c => c.ParentId == categoryId).Select(c => c.Id).ToListAsync();
        children.Add(categoryId);
        return children;
    }
}
