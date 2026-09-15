using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT06 – Quản lý kích cỡ (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class SizesController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public SizesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.UsageCounts = await _db.ProductVariants.GroupBy(v => v.SizeId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.Sizes = await _db.Sizes.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync();
        return View(new SizeFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Save(SizeFormViewModel model)
    {
        if (ModelState.IsValid && await _db.Sizes.AnyAsync(s => s.Name == model.Name.Trim() && s.Id != model.Id))
            ModelState.AddModelError(nameof(model.Name), "Tên size đã tồn tại.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        Size size;
        if (model.Id > 0)
        {
            size = await _db.Sizes.FindAsync(model.Id) ?? throw new KeyNotFoundException();
        }
        else
        {
            size = new Size();
            _db.Sizes.Add(size);
        }
        size.Name = model.Name.Trim();
        size.DisplayOrder = model.DisplayOrder;
        await _db.SaveChangesAsync();
        TempData["Success"] = model.Id > 0 ? "Đã cập nhật size." : "Đã thêm size.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var size = await _db.Sizes.FindAsync(id);
        if (size is null) return NotFound();
        if (await _db.ProductVariants.AnyAsync(v => v.SizeId == id))
        {
            TempData["Error"] = $"Không thể xóa size \"{size.Name}\": đang được dùng trong biến thể sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        _db.Sizes.Remove(size);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa size \"{size.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
