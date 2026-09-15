using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH01 – Trang chủ.</summary>
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICatalogService _catalog;
    private readonly IPricingService _pricing;
    private readonly ISettingService _settings;

    public HomeController(ApplicationDbContext db, ICatalogService catalog, IPricingService pricing, ISettingService settings)
    {
        _db = db;
        _catalog = catalog;
        _pricing = pricing;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var saleIds = await _pricing.GetProductIdsOnSaleAsync();
        var vm = new HomeViewModel
        {
            Banners = await _db.Banners.AsNoTracking()
                .Where(b => b.IsActive && b.Position == BannerPosition.HomeSlider)
                .OrderBy(b => b.DisplayOrder).ToListAsync(),
            RootCategories = await _db.Categories.AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.DisplayOrder).ToListAsync(),
            NewProducts = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts().Where(p => p.IsNew), "newest"), 8),
            SaleProducts = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts().Where(p => saleIds.Contains(p.Id)), "newest"), 8),
            BestSellers = await _catalog.ToCardsAsync(_catalog.ApplySort(_catalog.ActiveProducts(), "bestseller"), 8),
            Posts = await _db.Posts.AsNoTracking()
                .Where(p => p.IsPublished && p.Type == PostType.News)
                .OrderByDescending(p => p.PublishedAt).Take(3).ToListAsync(),
            FreeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000)
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
