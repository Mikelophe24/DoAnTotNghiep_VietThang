using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT04, QT05 – Quản lý sản phẩm, ảnh và biến thể (Admin, Employee).</summary>
public class ProductsController : AdminBaseController
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IHtmlSanitizer _sanitizer;

    public ProductsController(ApplicationDbContext db, IFileStorageService files, IHtmlSanitizer sanitizer)
    {
        _db = db;
        _files = files;
        _sanitizer = sanitizer;
    }

    // ---------------- Danh sách ----------------
    public async Task<IActionResult> Index(ProductListFilter filter)
    {
        var query = _db.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(p => p.Name.Contains(q) || p.Code.Contains(q));
        }
        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId || p.Category.ParentId == filter.CategoryId);
        if (filter.Active.HasValue)
            query = query.Where(p => p.IsActive == filter.Active);

        var projected = query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductListItem
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                CategoryName = p.Category.Name,
                Price = p.Price,
                ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Url).FirstOrDefault()
                          ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
                VariantCount = p.Variants.Count,
                TotalStock = p.Variants.Sum(v => v.StockQuantity),
                IsActive = p.IsActive,
                IsNew = p.IsNew,
                CreatedAt = p.CreatedAt
            });

        var page = await PagedList<ProductListItem>.CreateAsync(projected, filter.Page, PageSize);
        await LoadCategoryOptionsAsync(filter.CategoryId, includeRoots: true);
        ViewBag.Filter = filter;
        return View(page);
    }

    // ---------------- Thêm / sửa ----------------
    public async Task<IActionResult> Create()
    {
        await LoadCategoryOptionsAsync(null);
        return View("Form", new ProductFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadCategoryOptionsAsync(model.CategoryId);
            return View("Form", model);
        }

        var product = new Product();
        Apply(product, model);
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await SaveImagesAsync(product, model.ImageFiles);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\". Tiếp theo hãy tạo biến thể màu/size.";
        return RedirectToAction(nameof(Variants), new { id = product.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _db.Products.Include(x => x.Images.OrderBy(i => i.DisplayOrder)).Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        await LoadCategoryOptionsAsync(p.CategoryId);
        ViewBag.Product = p;
        ViewBag.Colors = new SelectList(await _db.Colors.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        return View("Form", new ProductFormViewModel
        {
            Id = p.Id, Code = p.Code, Name = p.Name, Slug = p.Slug, CategoryId = p.CategoryId, Material = p.Material,
            ShortDescription = p.ShortDescription, Description = p.Description, Price = p.Price,
            IsNew = p.IsNew, IsFeatured = p.IsFeatured, IsActive = p.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var product = await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id);
        if (product is null) return NotFound();

        await ValidateAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadCategoryOptionsAsync(model.CategoryId);
            ViewBag.Product = product;
            ViewBag.Colors = new SelectList(await _db.Colors.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            return View("Form", model);
        }

        Apply(product, model);
        product.UpdatedAt = DateTime.Now;
        await SaveImagesAsync(product, model.ImageFiles);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật sản phẩm \"{product.Name}\".";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id, string? returnUrl)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();
        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        var hasHistory = await _db.OrderDetails.AnyAsync(d => d.Variant.ProductId == id)
                      || await _db.GoodsReceiptDetails.AnyAsync(d => d.Variant.ProductId == id);
        if (hasHistory)
        {
            TempData["Error"] = "Sản phẩm đã phát sinh đơn hàng hoặc phiếu nhập nên không thể xóa. Hãy chuyển sang \"Ngừng bán\".";
            return RedirectToAction(nameof(Index));
        }

        foreach (var img in product.Images) _files.Delete(img.Url);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa sản phẩm \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- Ảnh ----------------
    [HttpPost]
    public async Task<IActionResult> AddImages(int id, List<IFormFile>? files, int? colorId)
    {
        var product = await _db.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (files is null || files.Count == 0)
        {
            TempData["Error"] = "Chưa chọn ảnh.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        await SaveImagesAsync(product, files, colorId);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm {files.Count} ảnh.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var image = await _db.ProductImages.FindAsync(id);
        if (image is null) return NotFound();
        var productId = image.ProductId;
        _files.Delete(image.Url);
        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        if (image.IsMain)
        {
            var next = await _db.ProductImages.Where(i => i.ProductId == productId).OrderBy(i => i.DisplayOrder).FirstOrDefaultAsync();
            if (next is not null) { next.IsMain = true; await _db.SaveChangesAsync(); }
        }
        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    public async Task<IActionResult> SetMainImage(int id)
    {
        var image = await _db.ProductImages.FindAsync(id);
        if (image is null) return NotFound();
        var siblings = await _db.ProductImages.Where(i => i.ProductId == image.ProductId).ToListAsync();
        foreach (var s in siblings) s.IsMain = s.Id == id;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = image.ProductId });
    }

    // ---------------- Biến thể ----------------
    public async Task<IActionResult> Variants(int id)
    {
        var product = await _db.Products
            .Include(p => p.Variants).ThenInclude(v => v.Color)
            .Include(p => p.Variants).ThenInclude(v => v.Size)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();

        ViewBag.Colors = await _db.Colors.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Sizes = await _db.Sizes.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync();
        ViewBag.UsedVariantIds = await _db.OrderDetails.Where(d => d.Variant.ProductId == id).Select(d => d.VariantId)
            .Union(_db.GoodsReceiptDetails.Where(d => d.Variant.ProductId == id).Select(d => d.VariantId))
            .Distinct().ToListAsync();
        return View(product);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateVariants(int id, int[] colorIds, int[] sizeIds)
    {
        var product = await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return NotFound();
        if (colorIds.Length == 0 || sizeIds.Length == 0)
        {
            TempData["Error"] = "Chọn ít nhất một màu và một size.";
            return RedirectToAction(nameof(Variants), new { id });
        }

        var colors = await _db.Colors.Where(c => colorIds.Contains(c.Id)).ToListAsync();
        var sizes = await _db.Sizes.Where(s => sizeIds.Contains(s.Id)).ToListAsync();
        var existing = product.Variants.Select(v => (v.ColorId, v.SizeId)).ToHashSet();
        var existingSkus = (await _db.ProductVariants.Select(v => v.Sku).ToListAsync()).ToHashSet();

        int added = 0;
        foreach (var color in colors)
        foreach (var size in sizes)
        {
            if (existing.Contains((color.Id, size.Id))) continue;
            var sku = $"{product.Code}-{SlugHelper.ToCodePart(color.Name)}-{SlugHelper.ToCodePart(size.Name)}";
            var unique = sku; int n = 1;
            while (existingSkus.Contains(unique)) unique = $"{sku}-{++n}";
            existingSkus.Add(unique);

            product.Variants.Add(new ProductVariant { ColorId = color.Id, SizeId = size.Id, Sku = unique, StockQuantity = 0 });
            added++;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = added > 0 ? $"Đã tạo {added} biến thể mới." : "Các biến thể đã tồn tại, không có gì thay đổi.";
        return RedirectToAction(nameof(Variants), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateVariant(VariantEditViewModel model)
    {
        var variant = await _db.ProductVariants.FindAsync(model.Id);
        if (variant is null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Giá không hợp lệ.";
            return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
        }
        variant.Price = model.Price;
        variant.IsActive = model.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật biến thể {variant.Sku}.";
        return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant is null) return NotFound();
        var used = await _db.OrderDetails.AnyAsync(d => d.VariantId == id) || await _db.GoodsReceiptDetails.AnyAsync(d => d.VariantId == id);
        if (used)
        {
            TempData["Error"] = $"Biến thể {variant.Sku} đã có lịch sử bán/nhập, chỉ có thể ngừng bán.";
            return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
        }
        _db.ProductVariants.Remove(variant);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa biến thể {variant.Sku}.";
        return RedirectToAction(nameof(Variants), new { id = variant.ProductId });
    }

    // ---------------- helpers ----------------
    private async Task LoadCategoryOptionsAsync(int? selected, bool includeRoots = false)
    {
        var categories = await _db.Categories.Include(c => c.Parent)
            .OrderBy(c => c.Parent != null ? c.Parent.DisplayOrder : c.DisplayOrder)
            .ThenBy(c => c.ParentId.HasValue ? 1 : 0)
            .ThenBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();
        var hasChildren = categories.Where(c => c.ParentId != null).Select(c => c.ParentId!.Value).ToHashSet();

        var items = categories
            .Where(c => includeRoots || !hasChildren.Contains(c.Id))
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Parent is null ? c.Name : $"{c.Parent.Name} › {c.Name}",
                Selected = c.Id == selected
            });
        ViewBag.Categories = items.ToList();
    }

    private async Task ValidateAsync(ProductFormViewModel model)
    {
        model.Code = model.Code.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Code == model.Code && p.Id != model.Id))
            ModelState.AddModelError(nameof(model.Code), $"Mã \"{model.Code}\" đã tồn tại.");

        var slug = string.IsNullOrWhiteSpace(model.Slug) ? SlugHelper.ToSlug(model.Name) : SlugHelper.ToSlug(model.Slug);
        if (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != model.Id))
            ModelState.AddModelError(nameof(model.Slug), $"Slug \"{slug}\" đã tồn tại.");
        model.Slug = slug;

        if (model.CategoryId.HasValue && await _db.Categories.AnyAsync(c => c.ParentId == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Hãy chọn danh mục cấp cuối (không phải danh mục cha).");
    }

    private void Apply(Product product, ProductFormViewModel model)
    {
        product.Code = model.Code;
        product.Name = model.Name.Trim();
        product.Slug = model.Slug!;
        product.CategoryId = model.CategoryId!.Value;
        product.Material = model.Material?.Trim();
        product.ShortDescription = model.ShortDescription?.Trim();
        product.Description = string.IsNullOrWhiteSpace(model.Description) ? null : _sanitizer.Sanitize(model.Description);
        product.Price = model.Price;
        product.IsNew = model.IsNew;
        product.IsFeatured = model.IsFeatured;
        product.IsActive = model.IsActive;
    }

    private async Task SaveImagesAsync(Product product, List<IFormFile>? files, int? colorId = null)
    {
        if (files is null) return;
        var order = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.DisplayOrder) + 1;
        foreach (var file in files.Where(f => f.Length > 0))
        {
            try
            {
                var url = await _files.SaveImageAsync(file, $"products/{product.Id}");
                product.Images.Add(new ProductImage
                {
                    Url = url,
                    ColorId = colorId,
                    DisplayOrder = order++,
                    IsMain = !product.Images.Any(i => i.IsMain)
                });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = $"{file.FileName}: {ex.Message}";
            }
        }
    }
}
