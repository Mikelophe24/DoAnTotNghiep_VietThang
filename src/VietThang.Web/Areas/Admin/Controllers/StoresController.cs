using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT19 – Hệ thống cửa hàng (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class StoresController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public StoresController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Stores.AsNoTracking().OrderBy(s => s.Name).ToListAsync());

    public IActionResult Create() => View("Form", new StoreFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(StoreFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var s = new Store();
        Apply(s, model);
        _db.Stores.Add(s);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm cửa hàng \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Stores.FindAsync(id);
        if (s is null) return NotFound();
        return View("Form", new StoreFormViewModel { Id = s.Id, Name = s.Name, Address = s.Address, Phone = s.Phone, OpeningHours = s.OpeningHours, MapEmbedUrl = s.MapEmbedUrl, IsActive = s.IsActive });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, StoreFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var s = await _db.Stores.FindAsync(id);
        if (s is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", model);
        Apply(s, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật cửa hàng \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Stores.FindAsync(id);
        if (s is null) return NotFound();
        _db.Stores.Remove(s);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa cửa hàng \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    private static void Apply(Store s, StoreFormViewModel m)
    {
        s.Name = m.Name.Trim();
        s.Address = m.Address.Trim();
        s.Phone = m.Phone?.Trim();
        s.OpeningHours = m.OpeningHours?.Trim();
        s.MapEmbedUrl = m.MapEmbedUrl?.Trim();
        s.IsActive = m.IsActive;
    }
}
