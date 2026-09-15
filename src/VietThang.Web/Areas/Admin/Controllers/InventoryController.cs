using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT14 – Tồn kho theo biến thể, điều chỉnh kiểm kê, lịch sử xuất nhập.</summary>
public class InventoryController : AdminBaseController
{
    private const int PageSize = 30;
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly ISettingService _settings;
    private readonly UserManager<ApplicationUser> _userManager;

    public InventoryController(ApplicationDbContext db, IInventoryService inventory, ISettingService settings, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventory = inventory;
        _settings = settings;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(InventoryFilter filter)
    {
        var lowThreshold = await _settings.GetIntAsync(SettingKeys.LowStockThreshold, 5);
        var query = _db.ProductVariants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(v => v.Sku.Contains(q) || v.Product.Name.Contains(q) || v.Product.Code.Contains(q));
        }
        if (filter.CategoryId.HasValue)
            query = query.Where(v => v.Product.CategoryId == filter.CategoryId || v.Product.Category.ParentId == filter.CategoryId);
        if (filter.LowOnly)
            query = query.Where(v => v.StockQuantity <= lowThreshold);

        var rows = query.OrderBy(v => v.Product.Name).ThenBy(v => v.Color.Name).ThenBy(v => v.Size.DisplayOrder)
            .Select(v => new InventoryRow
            {
                VariantId = v.Id, ProductId = v.ProductId, ProductName = v.Product.Name, Code = v.Product.Code,
                ColorName = v.Color.Name, SizeName = v.Size.Name, Sku = v.Sku,
                Stock = v.StockQuantity, CostPrice = v.CostPrice, Price = v.Price ?? v.Product.Price, IsActive = v.IsActive && v.Product.IsActive
            });

        ViewBag.Filter = filter;
        ViewBag.LowThreshold = lowThreshold;
        ViewBag.TotalStock = await query.SumAsync(v => v.StockQuantity);
        ViewBag.StockValue = await query.SumAsync(v => v.StockQuantity * v.CostPrice);
        ViewBag.LowCount = await _db.ProductVariants.CountAsync(v => v.IsActive && v.StockQuantity <= lowThreshold);
        ViewBag.Categories = new SelectList(await _db.Categories.Where(c => c.ParentId == null).OrderBy(c => c.DisplayOrder).ToListAsync(), "Id", "Name", filter.CategoryId);
        return View(await PagedList<InventoryRow>.CreateAsync(rows, filter.Page, PageSize));
    }

    [HttpPost]
    public async Task<IActionResult> Adjust(StockAdjustViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Số lượng không hợp lệ.";
        }
        else
        {
            try
            {
                await _inventory.AdjustStockAsync(model.VariantId, model.NewQuantity, model.Note, _userManager.GetUserId(User)!);
                TempData["Success"] = "Đã điều chỉnh tồn kho và ghi lịch sử.";
            }
            catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        }
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    public async Task<IActionResult> History(HistoryFilter filter)
    {
        var query = _db.InventoryTransactions.AsNoTracking()
            .Include(t => t.Variant).ThenInclude(v => v.Product)
            .Include(t => t.Variant).ThenInclude(v => v.Color)
            .Include(t => t.Variant).ThenInclude(v => v.Size)
            .Include(t => t.CreatedBy)
            .AsQueryable();
        if (filter.VariantId.HasValue) query = query.Where(t => t.VariantId == filter.VariantId);
        if (filter.Type.HasValue) query = query.Where(t => t.Type == filter.Type);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(t => t.Variant.Sku.Contains(q) || t.Variant.Product.Name.Contains(q) || (t.Note != null && t.Note.Contains(q)));
        }
        if (filter.From.HasValue) query = query.Where(t => t.CreatedAt >= filter.From.Value.Date);
        if (filter.To.HasValue) query = query.Where(t => t.CreatedAt < filter.To.Value.Date.AddDays(1));

        ViewBag.Filter = filter;
        if (filter.VariantId.HasValue)
            ViewBag.VariantLabel = await _db.ProductVariants.Where(v => v.Id == filter.VariantId)
                .Select(v => v.Product.Name + " – " + v.Color.Name + " / " + v.Size.Name + " (" + v.Sku + ")").FirstOrDefaultAsync();
        return View(await PagedList<InventoryTransaction>.CreateAsync(query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id), filter.Page, 50));
    }
}
