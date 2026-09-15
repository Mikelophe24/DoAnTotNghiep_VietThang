using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT17 – Banner trang chủ (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class BannersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;

    public BannersController(ApplicationDbContext db, IFileStorageService files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index()
        => View(await _db.Banners.AsNoTracking().OrderBy(b => b.Position).ThenBy(b => b.DisplayOrder).ToListAsync());

    public IActionResult Create() => View("Form", new BannerFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(BannerFormViewModel model)
    {
        if (model.ImageFile is null) ModelState.AddModelError(nameof(model.ImageFile), "Chọn ảnh banner.");
        if (!ModelState.IsValid) return View("Form", model);
        var b = new Banner();
        await ApplyAsync(b, model);
        _db.Banners.Add(b);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm banner.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        return View("Form", new BannerFormViewModel { Id = b.Id, Title = b.Title, LinkUrl = b.LinkUrl, Position = b.Position, DisplayOrder = b.DisplayOrder, IsActive = b.IsActive, ImageUrl = b.ImageUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, BannerFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        if (!ModelState.IsValid) { model.ImageUrl = b.ImageUrl; return View("Form", model); }
        await ApplyAsync(b, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật banner.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        b.IsActive = !b.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b is null) return NotFound();
        _files.Delete(b.ImageUrl);
        _db.Banners.Remove(b);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa banner.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ApplyAsync(Banner b, BannerFormViewModel m)
    {
        b.Title = m.Title?.Trim();
        b.LinkUrl = m.LinkUrl?.Trim();
        b.Position = m.Position;
        b.DisplayOrder = m.DisplayOrder;
        b.IsActive = m.IsActive;
        if (m.ImageFile is not null)
        {
            _files.Delete(b.ImageUrl);
            b.ImageUrl = await _files.SaveImageAsync(m.ImageFile, "banners");
        }
    }
}
