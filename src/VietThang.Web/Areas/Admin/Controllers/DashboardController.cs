using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT02 – Bảng điều khiển.</summary>
public class DashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;

    public DashboardController(ApplicationDbContext db, ISettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var low = await _settings.GetIntAsync(SettingKeys.LowStockThreshold, 5);

        ViewBag.OrdersToday = await _db.Orders.CountAsync(o => o.CreatedAt >= today);
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        ViewBag.RevenueToday = await _db.Orders.Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= today).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.RevenueMonth = await _db.Orders.Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.ProductCount = await _db.Products.CountAsync(p => p.IsActive);
        ViewBag.LowStock = await _db.ProductVariants.CountAsync(v => v.IsActive && v.StockQuantity <= low);
        ViewBag.CustomerCount = await _db.Users.CountAsync(u => u.Orders.Any());
        ViewBag.PendingReviews = await _db.Reviews.CountAsync(r => !r.IsApproved);
        ViewBag.NewContacts = await _db.Contacts.CountAsync(c => !c.IsHandled);

        ViewBag.RecentOrders = await _db.Orders.AsNoTracking().Include(o => o.Details)
            .OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync();
        ViewBag.LowStockItems = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.IsActive && v.StockQuantity <= low)
            .OrderBy(v => v.StockQuantity)
            .Select(v => new { v.Id, v.Sku, v.StockQuantity, ProductName = v.Product.Name, ColorName = v.Color.Name, SizeName = v.Size.Name })
            .Take(8).ToListAsync();
        return View();
    }
}
