using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers.Api;

/// <summary>API danh mục, sản phẩm, trang chủ cho Angular.</summary>
[Route("api/catalog")]
public class CatalogApiController : ApiControllerBase
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _db;
    private readonly ICatalogService _catalog;
    private readonly IPricingService _pricing;
    private readonly ISettingService _settings;

    public CatalogApiController(ApplicationDbContext db, ICatalogService catalog, IPricingService pricing, ISettingService settings)
    {
        _db = db;
        _catalog = catalog;
        _pricing = pricing;
        _settings = settings;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> Menu()
    {
        var roots = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == null)
            .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.DisplayOrder))
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                c.Id, c.Name, c.Slug, c.ImageUrl,
                Children = c.Children.Select(ch => new { ch.Id, ch.Name, ch.Slug }).ToList()
            }).ToListAsync();
        return Ok(roots);
    }

    [HttpGet("home")]
    public async Task<IActionResult> Home()
    {
        var saleIds = await _pricing.GetProductIdsOnSaleAsync();
        return Ok(new
        {
            banners = await _db.Banners.AsNoTracking().Where(b => b.IsActive && b.Position == BannerPosition.HomeSlider)
                .OrderBy(b => b.DisplayOrder).Select(b => new { b.Id, b.Title, b.ImageUrl, b.LinkUrl }).ToListAsync(),
            categories = await _db.Categories.AsNoTracking().Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.DisplayOrder).Select(c => new { c.Id, c.Name, c.Slug, c.ImageUrl }).ToListAsync(),
            newProducts = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts().Where(p => p.IsNew), "newest"), 8),
            saleProducts = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts().Where(p => saleIds.Contains(p.Id)), "newest"), 8),
            bestSellers = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts(), "bestseller"), 8),
            posts = await _db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Type == PostType.News)
                .OrderByDescending(p => p.PublishedAt).Take(3)
                .Select(p => new { p.Id, p.Title, p.Slug, p.Summary, p.ThumbnailUrl, p.PublishedAt }).ToListAsync(),
            freeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000)
        });
    }

    [HttpGet("filters")]
    public async Task<IActionResult> Filters() => Ok(new
    {
        colors = await _db.Colors.AsNoTracking().OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.HexCode }).ToListAsync(),
        sizes = await _db.Sizes.AsNoTracking().OrderBy(s => s.DisplayOrder).Select(s => new { s.Id, s.Name }).ToListAsync(),
        materials = await _db.Products.AsNoTracking().Where(p => p.IsActive && p.Material != null).Select(p => p.Material!).Distinct().OrderBy(m => m).ToListAsync()
    });

    [HttpGet("categories/{slug}")]
    public async Task<IActionResult> Category(string slug, [FromQuery] ProductFilter filter)
    {
        var category = await _db.Categories.AsNoTracking().Include(c => c.Parent).FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        if (category is null) return NotFound(new { message = "Không tìm thấy danh mục." });
        var ids = await _catalog.CategoryTreeIdsAsync(category.Id);
        var products = await ListAsync(_catalog.ActiveProducts().Where(p => ids.Contains(p.CategoryId)), filter);
        var subCategories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == (category.ParentId ?? category.Id))
            .OrderBy(c => c.DisplayOrder).Select(c => new { c.Id, c.Name, c.Slug }).ToListAsync();
        return Ok(new
        {
            category = new { category.Id, category.Name, category.Slug, category.Description, Parent = category.Parent is null ? null : new { category.Parent.Name, category.Parent.Slug } },
            subCategories,
            products
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] ProductFilter filter)
        => Ok(new { products = await ListAsync(_catalog.ActiveProducts(), filter) });

    [HttpGet("new")]
    public async Task<IActionResult> New([FromQuery] ProductFilter filter)
        => Ok(new { products = await ListAsync(_catalog.ActiveProducts().Where(p => p.IsNew), filter) });

    [HttpGet("sale")]
    public async Task<IActionResult> Sale([FromQuery] ProductFilter filter)
    {
        var saleIds = await _pricing.GetProductIdsOnSaleAsync();
        return Ok(new { products = await ListAsync(_catalog.ActiveProducts().Where(p => saleIds.Contains(p.Id)), filter) });
    }

    [HttpGet("products/{slug}")]
    public async Task<IActionResult> Product(string slug)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.Category).ThenInclude(c => c.Parent)
            .Include(x => x.Images.OrderBy(i => i.DisplayOrder))
            .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(v => v.Color)
            .Include(x => x.Variants.Where(v => v.IsActive)).ThenInclude(v => v.Size)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);
        if (p is null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

        await _db.Products.Where(x => x.Id == p.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));

        var promos = (await _pricing.GetActivePromotionsAsync(new[] { p.Id })).GetValueOrDefault(p.Id);
        var price = _pricing.Calculate(p.Price, promos);
        var variants = p.Variants.Select(v =>
        {
            var vp = _pricing.Calculate(v.Price ?? p.Price, promos);
            return new VariantOptionVm { Id = v.Id, ColorId = v.ColorId, SizeId = v.SizeId, Sku = v.Sku, OriginalPrice = vp.OriginalPrice, SalePrice = vp.SalePrice, Stock = v.StockQuantity };
        }).ToList();
        var reviews = await _db.Reviews.AsNoTracking().Where(r => r.ProductId == p.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt).Take(20)
            .Select(r => new { r.Id, UserName = r.User.FullName, r.Rating, r.Comment, r.CreatedAt }).ToListAsync();

        bool inWishlist = false;
        var userId = await TryGetJwtUserIdAsync();
        if (userId is not null) inWishlist = await _db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == p.Id);

        return Ok(new
        {
            p.Id, p.Code, p.Name, p.Slug, p.Material, p.ShortDescription, p.Description, p.SoldCount, p.IsNew,
            category = new { p.Category.Name, p.Category.Slug, Parent = p.Category.Parent is null ? null : new { p.Category.Parent.Name, p.Category.Parent.Slug } },
            price = new { price.OriginalPrice, price.SalePrice, price.DiscountPercent, price.HasDiscount },
            images = p.Images.Select(i => new { i.Id, i.Url, i.ColorId, i.IsMain }),
            colors = p.Variants.Select(v => v.Color).DistinctBy(c => c.Id).OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.HexCode }),
            sizes = p.Variants.Select(v => v.Size).DistinctBy(s => s.Id).OrderBy(s => s.DisplayOrder).Select(s => new { s.Id, s.Name }),
            variants,
            reviews,
            averageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1),
            related = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts().Where(x => x.CategoryId == p.CategoryId && x.Id != p.Id), "newest"), 8),
            inWishlist
        });
    }

    private async Task<object> ListAsync(IQueryable<Product> baseQuery, ProductFilter filter)
    {
        var paged = await _catalog.ToPagedCardsAsync(_catalog.ApplySort(_catalog.ApplyFilter(baseQuery, filter), filter.Sort), filter.Page, PageSize);
        return new { items = paged.Items, paged.PageIndex, paged.PageSize, paged.TotalCount, paged.TotalPages };
    }
}
