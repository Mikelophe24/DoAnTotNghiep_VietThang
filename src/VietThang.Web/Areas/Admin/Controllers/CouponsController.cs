using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT11 – Mã giảm giá (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class CouponsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public CouponsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Coupons.AsNoTracking().OrderByDescending(c => c.IsActive).ThenByDescending(c => c.EndDate).ToListAsync());

    public IActionResult Create() => View("Form", new CouponFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CouponFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        var c = new Coupon();
        Apply(c, model);
        _db.Coupons.Add(c);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo mã {c.Code}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is null) return NotFound();
        return View("Form", new CouponFormViewModel
        {
            Id = c.Id, Code = c.Code, Description = c.Description, DiscountType = c.DiscountType, DiscountValue = c.DiscountValue,
            MaxDiscountAmount = c.MaxDiscountAmount, MinOrderAmount = c.MinOrderAmount, UsageLimit = c.UsageLimit,
            StartDate = c.StartDate, EndDate = c.EndDate, IsActive = c.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CouponFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var c = await _db.Coupons.FindAsync(id);
        if (c is null) return NotFound();
        await ValidateAsync(model);
        if (!ModelState.IsValid) return View("Form", model);
        Apply(c, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật mã {c.Code}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c is null) return NotFound();
        if (await _db.Orders.AnyAsync(o => o.CouponId == id))
        {
            TempData["Error"] = $"Mã {c.Code} đã được dùng trong đơn hàng, chỉ có thể tắt.";
            return RedirectToAction(nameof(Index));
        }
        _db.Coupons.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa mã {c.Code}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(CouponFormViewModel m)
    {
        m.Code = m.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == m.Code && c.Id != m.Id)) ModelState.AddModelError(nameof(m.Code), "Mã đã tồn tại.");
        if (m.EndDate <= m.StartDate) ModelState.AddModelError(nameof(m.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (m.DiscountType == DiscountType.Percent && m.DiscountValue > 100) ModelState.AddModelError(nameof(m.DiscountValue), "Phần trăm giảm tối đa 100.");
    }

    private static void Apply(Coupon c, CouponFormViewModel m)
    {
        c.Code = m.Code;
        c.Description = m.Description?.Trim();
        c.DiscountType = m.DiscountType;
        c.DiscountValue = m.DiscountValue;
        c.MaxDiscountAmount = m.DiscountType == DiscountType.Percent ? m.MaxDiscountAmount : null;
        c.MinOrderAmount = m.MinOrderAmount;
        c.UsageLimit = m.UsageLimit;
        c.StartDate = m.StartDate.Date;
        c.EndDate = m.EndDate.Date.AddDays(1).AddSeconds(-1);
        c.IsActive = m.IsActive;
    }
}
