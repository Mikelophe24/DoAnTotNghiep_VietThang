using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT18 – Liên hệ từ khách.</summary>
public class ContactsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(bool? handled, int page = 1)
    {
        var query = _db.Contacts.AsNoTracking().Include(c => c.HandledBy).AsQueryable();
        if (handled.HasValue) query = query.Where(c => c.IsHandled == handled);
        ViewBag.Handled = handled;
        ViewBag.NewCount = await _db.Contacts.CountAsync(c => !c.IsHandled);
        return View(await PagedList<Contact>.CreateAsync(query.OrderBy(c => c.IsHandled).ThenByDescending(c => c.CreatedAt), page, 20));
    }

    [HttpPost]
    public async Task<IActionResult> MarkHandled(int id, string? returnUrl)
    {
        var c = await _db.Contacts.FindAsync(id);
        if (c is null) return NotFound();
        c.IsHandled = !c.IsHandled;
        c.HandledByUserId = c.IsHandled ? _userManager.GetUserId(User) : null;
        await _db.SaveChangesAsync();
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _db.Contacts.Where(c => c.Id == id).ExecuteDeleteAsync();
        TempData["Success"] = "Đã xóa liên hệ.";
        return RedirectToAction(nameof(Index));
    }
}
