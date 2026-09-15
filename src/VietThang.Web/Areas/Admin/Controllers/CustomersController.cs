using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT08 – Khách hàng: danh sách, chi tiết, khóa/mở tài khoản.</summary>
public class CustomersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private IQueryable<string> StaffIds() =>
        from ur in _db.UserRoles
        join r in _db.Roles on ur.RoleId equals r.Id
        where r.Name == SeedData.RoleAdmin || r.Name == SeedData.RoleEmployee
        select ur.UserId;

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var staff = StaffIds();
        var query = _db.Users.AsNoTracking().Where(u => !staff.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(u => u.FullName.Contains(s) || (u.Email != null && u.Email.Contains(s)) || (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
        }
        var projected = query.OrderByDescending(u => u.CreatedAt).Select(u => new CustomerListItem
        {
            Id = u.Id, FullName = u.FullName, Email = u.Email, Phone = u.PhoneNumber, CreatedAt = u.CreatedAt, IsActive = u.IsActive,
            OrderCount = u.Orders.Count(o => o.Status != OrderStatus.Cancelled),
            TotalSpent = u.Orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => (decimal?)o.TotalAmount) ?? 0
        });
        ViewBag.Q = q;
        return View(await PagedList<CustomerListItem>.CreateAsync(projected, page, 20));
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _db.Users.AsNoTracking().Include(u => u.Addresses).FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        ViewBag.Orders = await _db.Orders.AsNoTracking().Include(o => o.Details).Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAt).Take(30).ToListAsync();
        ViewBag.TotalSpent = await _db.Orders.Where(o => o.UserId == id && o.Status == OrderStatus.Completed).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.ReviewCount = await _db.Reviews.CountAsync(r => r.UserId == id);
        return View(user);
    }

    [HttpPost]
    [Authorize(Roles = SeedData.RoleAdmin)]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản, khách sẽ không đăng nhập được.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
