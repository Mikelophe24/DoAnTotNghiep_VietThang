using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT12 – Quản lý nhà cung cấp.</summary>
public class SuppliersController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public SuppliersController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.Name.Contains(q) || (s.Phone != null && s.Phone.Contains(q)));
        ViewBag.Q = q;
        ViewBag.ReceiptCounts = await _db.GoodsReceipts.GroupBy(r => r.SupplierId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);
        return View(await query.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).ToListAsync());
    }

    public IActionResult Create() => View("Form", new SupplierFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var s = new Supplier();
        Apply(s, model);
        _db.Suppliers.Add(s);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm nhà cung cấp \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return NotFound();
        return View("Form", new SupplierFormViewModel
        {
            Id = s.Id, Name = s.Name, ContactName = s.ContactName, Phone = s.Phone, Email = s.Email, Address = s.Address, IsActive = s.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, SupplierFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", model);
        Apply(s, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật nhà cung cấp \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Suppliers.FindAsync(id);
        if (s is null) return NotFound();
        if (await _db.GoodsReceipts.AnyAsync(r => r.SupplierId == id))
        {
            TempData["Error"] = "Nhà cung cấp đã có phiếu nhập, chỉ có thể ngừng hợp tác.";
            return RedirectToAction(nameof(Index));
        }
        _db.Suppliers.Remove(s);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa nhà cung cấp \"{s.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    private static void Apply(Supplier s, SupplierFormViewModel m)
    {
        s.Name = m.Name.Trim();
        s.ContactName = m.ContactName?.Trim();
        s.Phone = m.Phone?.Trim();
        s.Email = m.Email?.Trim();
        s.Address = m.Address?.Trim();
        s.IsActive = m.IsActive;
    }
}
