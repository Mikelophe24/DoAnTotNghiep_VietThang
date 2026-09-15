using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT03 – Quản lý danh mục (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class CategoriesController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;

    public CategoriesController(ApplicationDbContext db, IFileStorageService files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories
            .Include(c => c.Children)
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();
        var productCounts = await _db.Products.GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.ProductCounts = productCounts;
        return View(categories);
    }

    public async Task<IActionResult> Create(int? parentId)
    {
        await LoadParentsAsync(null);
        return View("Form", new CategoryFormViewModel { ParentId = parentId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(null);
            return View("Form", model);
        }

        var category = new Category();
        await ApplyAsync(category, model);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm danh mục \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        await LoadParentsAsync(id);
        return View("Form", new CategoryFormViewModel
        {
            Id = c.Id, Name = c.Name, Slug = c.Slug, ParentId = c.ParentId, Description = c.Description,
            DisplayOrder = c.DisplayOrder, IsActive = c.IsActive, ImageUrl = c.ImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var category = await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        if (model.ParentId == id)
            ModelState.AddModelError(nameof(model.ParentId), "Danh mục không thể là cha của chính nó.");
        if (model.ParentId.HasValue && category.Children.Any())
            ModelState.AddModelError(nameof(model.ParentId), "Danh mục đang có danh mục con nên phải là danh mục gốc.");
        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadParentsAsync(id);
            model.ImageUrl = category.ImageUrl;
            return View("Form", model);
        }

        await ApplyAsync(category, model);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật danh mục \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();
        if (category.Children.Any())
        {
            TempData["Error"] = "Không thể xóa: danh mục còn danh mục con.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
        {
            TempData["Error"] = "Không thể xóa: danh mục đang có sản phẩm. Hãy ẩn danh mục thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }
        _files.Delete(category.ImageUrl);
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa danh mục \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        category.IsActive = !category.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------
    private async Task LoadParentsAsync(int? excludeId)
    {
        var roots = await _db.Categories
            .Where(c => c.ParentId == null && c.Id != excludeId)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        ViewBag.Parents = new SelectList(roots, "Id", "Name");
    }

    private async Task ValidateAsync(CategoryFormViewModel model)
    {
        var slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugHelper.ToSlug(model.Name) : SlugHelper.ToSlug(model.Slug);
        if (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != model.Id))
            ModelState.AddModelError(nameof(model.Slug), $"Slug \"{slug}\" đã tồn tại.");
        model.Slug = slug;
    }

    private async Task ApplyAsync(Category category, CategoryFormViewModel model)
    {
        category.Name = model.Name.Trim();
        category.Slug = model.Slug!;
        category.ParentId = model.ParentId;
        category.Description = model.Description?.Trim();
        category.DisplayOrder = model.DisplayOrder;
        category.IsActive = model.IsActive;
        if (model.ImageFile is not null)
        {
            _files.Delete(category.ImageUrl);
            category.ImageUrl = await _files.SaveImageAsync(model.ImageFile, "categories");
        }
    }
}
