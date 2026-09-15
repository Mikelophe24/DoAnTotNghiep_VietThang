using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH02, KH03, KH04 – Danh mục, tìm kiếm, lọc, chi tiết sản phẩm.</summary>
public class ProductsController : Controller
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _db;
    private readonly ICatalogService _catalog;
    private readonly IPricingService _pricing;

    public ProductsController(ApplicationDbContext db, ICatalogService catalog, IPricingService pricing)
    {
        _db = db;
        _catalog = catalog;
        _pricing = pricing;
    }

    [HttpGet("danh-muc/{slug}")]
    public async Task<IActionResult> Category(string slug, ProductFilter filter)
    {
        var category = await _db.Categories.AsNoTracking().Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
        if (category is null) return NotFound();

        var ids = await _catalog.CategoryTreeIdsAsync(category.Id);
        var query = _catalog.ActiveProducts().Where(p => ids.Contains(p.CategoryId));

        var breadcrumb = new List<(string, string)>();
        if (category.Parent is not null) breadcrumb.Add((category.Parent.Name, Url.Action(nameof(Category), new { slug = category.Parent.Slug })!));
        breadcrumb.Add((category.Name, Url.Action(nameof(Category), new { slug = category.Slug })!));

        var vm = await BuildListAsync(query, filter, category.Name, breadcrumb);
        vm.Category = category;
        vm.Description = category.Description;
        vm.SubCategories = await _db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == (category.ParentId ?? category.Id))
            .OrderBy(c => c.DisplayOrder).ToListAsync();
        return View("List", vm);
    }

    [HttpGet("tim-kiem")]
    public async Task<IActionResult> Search(ProductFilter filter)
    {
        var title = string.IsNullOrWhiteSpace(filter.Q) ? "Tìm kiếm" : $"Kết quả cho \"{filter.Q.Trim()}\"";
        var vm = await BuildListAsync(_catalog.ActiveProducts(), filter, title, new() { ("Tìm kiếm", Url.Action(nameof(Search))!) });
        return View("List", vm);
    }

    [HttpGet("hang-moi-ve")]
    public async Task<IActionResult> New(ProductFilter filter)
    {
        var vm = await BuildListAsync(_catalog.ActiveProducts().Where(p => p.IsNew), filter, "Hàng mới về", new() { ("Hàng mới về", Url.Action(nameof(New))!) });
        return View("List", vm);
    }

    [HttpGet("sale")]
    public async Task<IActionResult> Sale(ProductFilter filter)
    {
        var saleIds = await _pricing.GetProductIdsOnSaleAsync();
        var vm = await BuildListAsync(_catalog.ActiveProducts().Where(p => saleIds.Contains(p.Id)), filter, "Đang khuyến mãi", new() { ("Sale", Url.Action(nameof(Sale))!) });
        return View("List", vm);
    }

    [HttpGet("san-pham/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category).ThenInclude(c => c.Parent)
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Variants.Where(v => v.IsActive)).ThenInclude(v => v.Color)
            .Include(p => p.Variants.Where(v => v.IsActive)).ThenInclude(v => v.Size)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);
        if (product is null) return NotFound();

        await _db.Products.Where(p => p.Id == product.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1));

        var promos = (await _pricing.GetActivePromotionsAsync(new[] { product.Id })).GetValueOrDefault(product.Id);
        var variants = product.Variants.Select(v =>
        {
            var price = _pricing.Calculate(v.Price ?? product.Price, promos);
            return new VariantOptionVm
            {
                Id = v.Id, ColorId = v.ColorId, SizeId = v.SizeId, Sku = v.Sku,
                OriginalPrice = price.OriginalPrice, SalePrice = price.SalePrice, Stock = v.StockQuantity
            };
        }).ToList();

        var reviews = await _db.Reviews.AsNoTracking().Include(r => r.User)
            .Where(r => r.ProductId == product.Id && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt).Take(20).ToListAsync();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Price = _pricing.Calculate(product.Price, promos),
            Colors = product.Variants.Select(v => v.Color).DistinctBy(c => c.Id).OrderBy(c => c.Name).ToList(),
            Sizes = product.Variants.Select(v => v.Size).DistinctBy(s => s.Id).OrderBy(s => s.DisplayOrder).ToList(),
            Variants = variants,
            VariantsJson = JsonSerializer.Serialize(variants, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            Reviews = reviews,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(r => r.Rating), 1),
            Related = await _catalog.ToCardsAsync(
                _catalog.ApplySort(_catalog.ActiveProducts().Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id), "newest"), 8)
        };
        return View(vm);
    }

    private async Task<ProductListViewModel> BuildListAsync(IQueryable<Product> baseQuery, ProductFilter filter, string title, List<(string, string)> breadcrumb)
    {
        var filtered = _catalog.ApplySort(_catalog.ApplyFilter(baseQuery, filter), filter.Sort);
        return new ProductListViewModel
        {
            Title = title,
            Breadcrumb = breadcrumb,
            Filter = filter,
            Products = await _catalog.ToPagedCardsAsync(filtered, filter.Page, PageSize),
            Colors = await _db.Colors.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            Sizes = await _db.Sizes.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync(),
            Materials = await baseQuery.Where(p => p.Material != null).Select(p => p.Material!).Distinct().OrderBy(m => m).ToListAsync()
        };
    }
}
