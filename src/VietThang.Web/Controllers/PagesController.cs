using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH15, KH16 – Hệ thống cửa hàng và liên hệ.</summary>
public class PagesController : Controller
{
    private readonly ApplicationDbContext _db;
    public PagesController(ApplicationDbContext db) => _db = db;

    [HttpGet("he-thong-cua-hang")]
    public async Task<IActionResult> Stores()
        => View(await _db.Stores.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync());

    [HttpGet("lien-he")]
    public IActionResult Contact() => View(new ContactViewModel());

    [HttpPost("lien-he")]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Contacts.Add(new Contact
        {
            FullName = model.FullName.Trim(),
            Email = model.Email?.Trim(),
            Phone = model.Phone?.Trim(),
            Subject = model.Subject?.Trim(),
            Message = model.Message.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn. Cửa hàng sẽ liên hệ lại trong thời gian sớm nhất.";
        return RedirectToAction(nameof(Contact));
    }
}
