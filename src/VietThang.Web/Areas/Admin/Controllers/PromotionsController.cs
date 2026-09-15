using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT10 – Chương trình khuyến mãi theo sản phẩm (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class PromotionsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public PromotionsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var list = await _db.Promotions.AsNoTracking().Include(p => p.PromotionProducts)
            .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.EndDate).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        await LoadProductsAsync();
        return View("Form", new PromotionFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PromotionFormViewModel model)
    {
        Validate(model);
        if (!ModelState.IsValid) { await LoadProductsAsync(); return View("Form", model); }
        var promo = new Promotion();
        Apply(promo, model);
        _db.Promotions.Add(promo);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo chương trình \"{promo.Name}\" cho {model.ProductIds.Count} sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Promotions.Include(x => x.PromotionProducts).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        await LoadProductsAsync();
        return View("Form", new PromotionFormViewModel
        {
            Id = p.Id, Name = p.Name, DiscountType = p.DiscountType, DiscountValue = p.DiscountValue,
            StartDate = p.StartDate, EndDate = p.EndDate, IsActive = p.IsActive,
            ProductIds = p.PromotionProducts.Select(pp => pp.ProductId).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, PromotionFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var p = await _db.Promotions.Include(x => x.PromotionProducts).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        Validate(model);
        if (!ModelState.IsValid) { await LoadProductsAsync(); return View("Form", model); }
        Apply(p, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật chương trình \"{p.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var p = await _db.Promotions.FindAsync(id);
        if (p is null) return NotFound();
        p.IsActive = !p.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Promotions.FindAsync(id);
        if (p is null) return NotFound();
        _db.Promotions.Remove(p);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa chương trình \"{p.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    private void Validate(PromotionFormViewModel m)
    {
        if (m.EndDate <= m.StartDate) ModelState.AddModelError(nameof(m.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
        if (m.DiscountType == Models.Enums.DiscountType.Percent && m.DiscountValue > 100) ModelState.AddModelError(nameof(m.DiscountValue), "Phần trăm giảm tối đa 100.");
        if (m.ProductIds.Count == 0) ModelState.AddModelError(nameof(m.ProductIds), "Chọn ít nhất một sản phẩm.");
    }

    private static void Apply(Promotion p, PromotionFormViewModel m)
    {
        p.Name = m.Name.Trim();
        p.DiscountType = m.DiscountType;
        p.DiscountValue = m.DiscountValue;
        p.StartDate = m.StartDate.Date;
        p.EndDate = m.EndDate.Date.AddDays(1).AddSeconds(-1);
        p.IsActive = m.IsActive;
        p.PromotionProducts.Clear();
        foreach (var id in m.ProductIds.Distinct()) p.PromotionProducts.Add(new PromotionProduct { ProductId = id });
    }

    private async Task LoadProductsAsync()
    {
        ViewBag.Products = await _db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive).OrderBy(p => p.Category.Name).ThenBy(p => p.Name)
            .Select(p => new { p.Id, p.Code, p.Name, p.Price, CategoryName = p.Category.Name }).ToListAsync();
    }
}
