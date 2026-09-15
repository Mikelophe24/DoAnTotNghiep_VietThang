using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT06 – Quản lý màu sắc (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class ColorsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public ColorsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.UsageCounts = await _db.ProductVariants.GroupBy(v => v.ColorId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.Colors = await _db.Colors.OrderBy(c => c.Name).ToListAsync();
        return View(new ColorFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Save(ColorFormViewModel model)
    {
        if (ModelState.IsValid && await _db.Colors.AnyAsync(c => c.Name == model.Name.Trim() && c.Id != model.Id))
            ModelState.AddModelError(nameof(model.Name), "Tên màu đã tồn tại.");
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        Color color;
        if (model.Id > 0)
        {
            color = await _db.Colors.FindAsync(model.Id) ?? throw new KeyNotFoundException();
        }
        else
        {
            color = new Color();
            _db.Colors.Add(color);
        }
        color.Name = model.Name.Trim();
        color.HexCode = model.HexCode?.ToUpperInvariant();
        await _db.SaveChangesAsync();
        TempData["Success"] = model.Id > 0 ? "Đã cập nhật màu." : "Đã thêm màu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var color = await _db.Colors.FindAsync(id);
        if (color is null) return NotFound();
        if (await _db.ProductVariants.AnyAsync(v => v.ColorId == id))
        {
            TempData["Error"] = $"Không thể xóa màu \"{color.Name}\": đang được dùng trong biến thể sản phẩm.";
            return RedirectToAction(nameof(Index));
        }
        _db.Colors.Remove(color);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa màu \"{color.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
