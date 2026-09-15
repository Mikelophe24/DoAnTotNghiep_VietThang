using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT15 – Duyệt đánh giá sản phẩm.</summary>
public class ReviewsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    public ReviewsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(bool? approved, int page = 1)
    {
        var query = _db.Reviews.AsNoTracking().Include(r => r.Product).Include(r => r.User).AsQueryable();
        if (approved.HasValue) query = query.Where(r => r.IsApproved == approved);
        ViewBag.Approved = approved;
        ViewBag.PendingCount = await _db.Reviews.CountAsync(r => !r.IsApproved);
        return View(await PagedList<Review>.CreateAsync(query.OrderBy(r => r.IsApproved).ThenByDescending(r => r.CreatedAt), page, 20));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id, string? returnUrl)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r is null) return NotFound();
        r.IsApproved = !r.IsApproved;
        await _db.SaveChangesAsync();
        TempData["Success"] = r.IsApproved ? "Đã duyệt đánh giá, sẽ hiển thị trên trang sản phẩm." : "Đã ẩn đánh giá.";
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        await _db.Reviews.Where(r => r.Id == id).ExecuteDeleteAsync();
        TempData["Success"] = "Đã xóa đánh giá.";
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }
}
