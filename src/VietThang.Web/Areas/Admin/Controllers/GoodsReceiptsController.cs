using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT13 – Phiếu nhập hàng: lập nháp, thêm dòng, duyệt nhập kho.</summary>
public class GoodsReceiptsController : AdminBaseController
{
    private const int PageSize = 20;
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventory;
    private readonly UserManager<ApplicationUser> _userManager;

    public GoodsReceiptsController(ApplicationDbContext db, IInventoryService inventory, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _inventory = inventory;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(ReceiptListFilter filter)
    {
        var query = _db.GoodsReceipts.AsNoTracking().Include(r => r.Supplier).Include(r => r.CreatedBy).AsQueryable();
        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status);
        if (filter.SupplierId.HasValue) query = query.Where(r => r.SupplierId == filter.SupplierId);
        if (filter.From.HasValue) query = query.Where(r => r.ReceiptDate >= filter.From.Value.Date);
        if (filter.To.HasValue) query = query.Where(r => r.ReceiptDate < filter.To.Value.Date.AddDays(1));

        ViewBag.Filter = filter;
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.OrderBy(s => s.Name).ToListAsync(), "Id", "Name", filter.SupplierId);
        return View(await PagedList<GoodsReceipt>.CreateAsync(query.OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.Id), filter.Page, PageSize));
    }

    public async Task<IActionResult> Create(int? supplierId)
    {
        await LoadSuppliersAsync(supplierId);
        return View(new GoodsReceiptFormViewModel { SupplierId = supplierId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(GoodsReceiptFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadSuppliersAsync(model.SupplierId);
            return View(model);
        }
        var receipt = new GoodsReceipt
        {
            Code = await _inventory.GenerateReceiptCodeAsync(),
            SupplierId = model.SupplierId!.Value,
            ReceiptDate = model.ReceiptDate,
            Note = model.Note?.Trim(),
            CreatedByUserId = _userManager.GetUserId(User)!,
            Status = ReceiptStatus.Draft
        };
        _db.GoodsReceipts.Add(receipt);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã tạo phiếu nháp {receipt.Code}. Thêm dòng hàng rồi bấm Duyệt nhập kho.";
        return RedirectToAction(nameof(Edit), new { id = receipt.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var receipt = await LoadReceiptAsync(id);
        if (receipt is null) return NotFound();
        await LoadSuppliersAsync(receipt.SupplierId);
        ViewBag.Products = new SelectList(await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name)
            .Select(p => new { p.Id, Name = p.Code + " – " + p.Name }).ToListAsync(), "Id", "Name");
        return View(receipt);
    }

    /// <summary>JSON danh sách biến thể của một sản phẩm cho form thêm dòng.</summary>
    [HttpGet]
    public async Task<IActionResult> Variants(int productId)
    {
        var list = await _db.ProductVariants.AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderBy(v => v.Color.Name).ThenBy(v => v.Size.DisplayOrder)
            .Select(v => new { id = v.Id, label = v.Color.Name + " / " + v.Size.Name + " (" + v.Sku + ")", stock = v.StockQuantity, costPrice = v.CostPrice })
            .ToListAsync();
        return Json(list);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateHeader(GoodsReceiptFormViewModel model)
    {
        var receipt = await _db.GoodsReceipts.FindAsync(model.Id);
        if (receipt is null) return NotFound();
        if (receipt.Status != ReceiptStatus.Draft) { TempData["Error"] = "Phiếu đã duyệt, không sửa được."; return RedirectToAction(nameof(Edit), new { id = model.Id }); }
        if (model.SupplierId.HasValue) receipt.SupplierId = model.SupplierId.Value;
        receipt.ReceiptDate = model.ReceiptDate;
        receipt.Note = model.Note?.Trim();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu thông tin phiếu.";
        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    public async Task<IActionResult> AddLine(ReceiptLineViewModel model)
    {
        var receipt = await _db.GoodsReceipts.Include(r => r.Details).FirstOrDefaultAsync(r => r.Id == model.ReceiptId);
        if (receipt is null) return NotFound();
        if (receipt.Status != ReceiptStatus.Draft) { TempData["Error"] = "Phiếu đã duyệt, không thêm dòng được."; return RedirectToAction(nameof(Edit), new { id = model.ReceiptId }); }
        if (!ModelState.IsValid || !model.VariantId.HasValue)
        {
            TempData["Error"] = "Dữ liệu dòng hàng không hợp lệ: " + string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Edit), new { id = model.ReceiptId });
        }
        var existing = receipt.Details.FirstOrDefault(d => d.VariantId == model.VariantId);
        if (existing is not null)
        {
            existing.Quantity += model.Quantity;
            existing.UnitCost = model.UnitCost;
        }
        else
        {
            receipt.Details.Add(new GoodsReceiptDetail { VariantId = model.VariantId.Value, Quantity = model.Quantity, UnitCost = model.UnitCost });
        }
        receipt.TotalAmount = receipt.Details.Sum(d => d.Quantity * d.UnitCost);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = model.ReceiptId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveLine(int id)
    {
        var line = await _db.GoodsReceiptDetails.Include(d => d.GoodsReceipt).ThenInclude(r => r.Details).FirstOrDefaultAsync(d => d.Id == id);
        if (line is null) return NotFound();
        var receipt = line.GoodsReceipt;
        if (receipt.Status != ReceiptStatus.Draft) { TempData["Error"] = "Phiếu đã duyệt, không xóa dòng được."; return RedirectToAction(nameof(Edit), new { id = receipt.Id }); }
        receipt.Details.Remove(line);
        _db.GoodsReceiptDetails.Remove(line);
        receipt.TotalAmount = receipt.Details.Sum(d => d.Quantity * d.UnitCost);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = receipt.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _inventory.CompleteReceiptAsync(id, _userManager.GetUserId(User)!);
            TempData["Success"] = "Đã duyệt nhập kho, tồn kho đã được cộng.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var receipt = await _db.GoodsReceipts.FindAsync(id);
        if (receipt is null) return NotFound();
        if (receipt.Status != ReceiptStatus.Draft) { TempData["Error"] = "Chỉ hủy được phiếu nháp."; return RedirectToAction(nameof(Edit), new { id }); }
        receipt.Status = ReceiptStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã hủy phiếu {receipt.Code}.";
        return RedirectToAction(nameof(Index));
    }

    private Task<GoodsReceipt?> LoadReceiptAsync(int id) =>
        _db.GoodsReceipts
            .Include(r => r.Supplier)
            .Include(r => r.CreatedBy)
            .Include(r => r.Details).ThenInclude(d => d.Variant).ThenInclude(v => v.Product)
            .Include(r => r.Details).ThenInclude(d => d.Variant).ThenInclude(v => v.Color)
            .Include(r => r.Details).ThenInclude(d => d.Variant).ThenInclude(v => v.Size)
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task LoadSuppliersAsync(int? selected)
    {
        ViewBag.Suppliers = new SelectList(await _db.Suppliers.Where(s => s.IsActive || s.Id == selected).OrderBy(s => s.Name).ToListAsync(), "Id", "Name", selected);
    }
}
