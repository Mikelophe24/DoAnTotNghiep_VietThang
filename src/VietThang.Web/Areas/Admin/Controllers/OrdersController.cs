using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT07 – Quản lý đơn hàng (UC-12).</summary>
public class OrdersController : AdminBaseController
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IOrderService _orders;
    private readonly ISettingService _settings;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(ApplicationDbContext db, IOrderService orders, ISettingService settings, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _orders = orders;
        _settings = settings;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(OrderListFilter filter)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Details).AsQueryable();
        if (filter.Status.HasValue) query = query.Where(o => o.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(o => o.OrderCode.Contains(q) || o.Phone.Contains(q) || o.CustomerName.Contains(q));
        }
        if (filter.From.HasValue) query = query.Where(o => o.CreatedAt >= filter.From.Value.Date);
        if (filter.To.HasValue) query = query.Where(o => o.CreatedAt < filter.To.Value.Date.AddDays(1));

        ViewBag.Filter = filter;
        ViewBag.Counts = await _db.Orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(await PagedList<Order>.CreateAsync(query.OrderByDescending(o => o.CreatedAt), filter.Page, PageSize));
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Details)
            .Include(o => o.StatusHistories.OrderBy(h => h.ChangedAt)).ThenInclude(h => h.ChangedBy)
            .Include(o => o.User)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        ViewBag.BankAccount = await _settings.GetAsync(SettingKeys.BankAccount);
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(ChangeStatusViewModel model)
    {
        if (model.Status == OrderStatus.Cancelled && string.IsNullOrWhiteSpace(model.Note))
        {
            TempData["Error"] = "Nhập lý do hủy đơn.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        var (ok, message) = await _orders.ChangeStatusAsync(model.Id, model.Status, model.Note?.Trim(), _userManager.GetUserId(User));
        TempData[ok ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();
        if (order.Status == OrderStatus.Cancelled) { TempData["Error"] = "Đơn đã hủy."; return RedirectToAction(nameof(Details), new { id }); }
        order.PaymentStatus = PaymentStatus.Paid;
        order.StatusHistories.Add(new OrderStatusHistory { FromStatus = order.Status, ToStatus = order.Status, Note = "Đã nhận thanh toán chuyển khoản", ChangedByUserId = _userManager.GetUserId(User) });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã đánh dấu đơn đã thanh toán.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Print(int id)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();
        ViewBag.StoreName = await _settings.GetAsync(SettingKeys.StoreName);
        ViewBag.Hotline = await _settings.GetAsync(SettingKeys.Hotline);
        return View(order);
    }
}
