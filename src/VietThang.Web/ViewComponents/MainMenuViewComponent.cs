using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;

namespace VietThang.Web.ViewComponents;

/// <summary>Menu danh mục hai cấp ở header, cache 10 phút.</summary>
public class MainMenuViewComponent : ViewComponent
{
    public const string CacheKey = "menu.categories";
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public MainMenuViewComponent(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _db.Categories.AsNoTracking()
                .Where(c => c.IsActive && c.ParentId == null)
                .Include(c => c.Children.Where(ch => ch.IsActive).OrderBy(ch => ch.DisplayOrder))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        });
        return View(categories ?? new List<Category>());
    }
}
