using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _db;

    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        ViewBag.OrdersToday = await _db.Orders.CountAsync(o => o.CreatedAt >= today);
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        ViewBag.RevenueToday = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= today)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.ProductCount = await _db.Products.CountAsync(p => p.IsActive);
        ViewBag.LowStock = await _db.ProductVariants.CountAsync(v => v.IsActive && v.StockQuantity <= 5);
        return View();
    }
}
