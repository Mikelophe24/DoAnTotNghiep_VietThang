using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH08 – Tra cứu đơn hàng theo mã và số điện thoại (khách vãng lai).</summary>
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    public OrdersController(ApplicationDbContext db) => _db = db;

    [HttpGet("tra-cuu-don-hang")]
    public IActionResult Lookup(string? code) => View(new OrderLookupViewModel { OrderCode = code ?? string.Empty });

    [HttpPost("tra-cuu-don-hang")]
    public async Task<IActionResult> Lookup(OrderLookupViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var code = model.OrderCode.Trim().ToUpperInvariant();
        var phone = model.Phone.Trim();
        model.Order = await _db.Orders.AsNoTracking()
            .Include(o => o.Details)
            .Include(o => o.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.OrderCode == code && o.Phone == phone);
        model.Searched = true;
        return View(model);
    }
}
