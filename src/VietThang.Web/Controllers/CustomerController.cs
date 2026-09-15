using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH10–KH13 – Tài khoản khách hàng: hồ sơ, mật khẩu, sổ địa chỉ, đơn hàng, đánh giá, yêu thích.</summary>
[Authorize]
public class CustomerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orders;
    private readonly ICatalogService _catalog;

    public CustomerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, IOrderService orders, ICatalogService catalog)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _orders = orders;
        _catalog = catalog;
    }

    private string UserId => _userManager.GetUserId(User)!;

    // ---------- Hồ sơ ----------
    [HttpGet("tai-khoan")]
    public async Task<IActionResult> Profile()
    {
        var u = (await _userManager.GetUserAsync(User))!;
        return View(new ProfileViewModel { FullName = u.FullName, Email = u.Email, PhoneNumber = u.PhoneNumber ?? "", Gender = u.Gender, DateOfBirth = u.DateOfBirth });
    }

    [HttpPost("tai-khoan")]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var u = (await _userManager.GetUserAsync(User))!;
        model.Email = u.Email;
        if (!ModelState.IsValid) return View(model);
        u.FullName = model.FullName.Trim();
        u.PhoneNumber = model.PhoneNumber.Trim();
        u.Gender = model.Gender;
        u.DateOfBirth = model.DateOfBirth;
        await _userManager.UpdateAsync(u);
        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet("tai-khoan/doi-mat-khau")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost("tai-khoan/doi-mat-khau")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var u = (await _userManager.GetUserAsync(User))!;
        var result = await _userManager.ChangePasswordAsync(u, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Errors.Any(e => e.Code == "PasswordMismatch") ? "Mật khẩu hiện tại không đúng." : string.Join(" ", result.Errors.Select(e => e.Description)));
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(u);
        TempData["Success"] = "Đã đổi mật khẩu.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- Sổ địa chỉ ----------
    [HttpGet("tai-khoan/dia-chi")]
    public async Task<IActionResult> Addresses()
        => View(await _db.Addresses.AsNoTracking().Where(a => a.UserId == UserId).OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync());

    [HttpGet("tai-khoan/dia-chi/them")]
    public IActionResult AddressForm() => View(new AddressFormViewModel());

    [HttpGet("tai-khoan/dia-chi/sua/{id:int}")]
    public async Task<IActionResult> EditAddress(int id)
    {
        var a = await _db.Addresses.FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId);
        if (a is null) return NotFound();
        return View("AddressForm", new AddressFormViewModel
        {
            Id = a.Id, ReceiverName = a.ReceiverName, Phone = a.Phone, Province = a.Province, District = a.District, Ward = a.Ward, Street = a.Street, IsDefault = a.IsDefault
        });
    }

    [HttpPost("tai-khoan/dia-chi/luu")]
    public async Task<IActionResult> SaveAddress(AddressFormViewModel model)
    {
        if (!ModelState.IsValid) return View("AddressForm", model);
        Address address;
        if (model.Id > 0)
        {
            address = await _db.Addresses.FirstOrDefaultAsync(x => x.Id == model.Id && x.UserId == UserId) ?? throw new KeyNotFoundException();
        }
        else
        {
            address = new Address { UserId = UserId };
            _db.Addresses.Add(address);
        }
        address.ReceiverName = model.ReceiverName.Trim();
        address.Phone = model.Phone.Trim();
        address.Province = model.Province.Trim();
        address.District = model.District.Trim();
        address.Ward = model.Ward.Trim();
        address.Street = model.Street.Trim();
        var hasAny = await _db.Addresses.AnyAsync(a => a.UserId == UserId && a.Id != model.Id);
        address.IsDefault = model.IsDefault || !hasAny;
        if (address.IsDefault)
            await _db.Addresses.Where(a => a.UserId == UserId && a.Id != model.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã lưu địa chỉ.";
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("tai-khoan/dia-chi/xoa/{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        await _db.Addresses.Where(a => a.Id == id && a.UserId == UserId).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("tai-khoan/dia-chi/mac-dinh/{id:int}")]
    public async Task<IActionResult> SetDefaultAddress(int id)
    {
        await _db.Addresses.Where(a => a.UserId == UserId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, a => a.Id == id));
        return RedirectToAction(nameof(Addresses));
    }

    // ---------- Đơn hàng ----------
    [HttpGet("tai-khoan/don-hang")]
    public async Task<IActionResult> Orders(int page = 1)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Details).Where(o => o.UserId == UserId).OrderByDescending(o => o.CreatedAt);
        return View(await PagedList<Order>.CreateAsync(query, page, 10));
    }

    [HttpGet("tai-khoan/don-hang/{code}")]
    public async Task<IActionResult> OrderDetails(string code)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Details)
            .Include(o => o.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound();
        ViewBag.ReviewedProductIds = await _db.Reviews.Where(r => r.OrderId == order.Id).Select(r => r.ProductId).ToListAsync();
        ViewBag.VariantProductIds = await _db.ProductVariants.Where(v => order.Details.Select(d => d.VariantId).Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.ProductId);
        return View(order);
    }

    [HttpPost("tai-khoan/don-hang/{code}/huy")]
    public async Task<IActionResult> CancelOrder(string code, string? reason)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Chỉ hủy được đơn đang chờ xác nhận. Vui lòng gọi hotline để được hỗ trợ.";
            return RedirectToAction(nameof(OrderDetails), new { code });
        }
        var (ok, message) = await _orders.ChangeStatusAsync(order.Id, OrderStatus.Cancelled, "Khách tự hủy" + (string.IsNullOrWhiteSpace(reason) ? "" : ": " + reason.Trim()), UserId);
        TempData[ok ? "Success" : "Error"] = ok ? "Đã hủy đơn hàng." : message;
        return RedirectToAction(nameof(OrderDetails), new { code });
    }

    [HttpPost("tai-khoan/don-hang/{code}/danh-gia")]
    public async Task<IActionResult> Review(string code, ReviewFormViewModel model)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details).FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.Completed) { TempData["Error"] = "Chỉ đánh giá được khi đơn đã hoàn thành."; return RedirectToAction(nameof(OrderDetails), new { code }); }
        var inOrder = await _db.ProductVariants.AnyAsync(v => v.ProductId == model.ProductId && order.Details.Select(d => d.VariantId).Contains(v.Id));
        if (!inOrder || !ModelState.IsValid) { TempData["Error"] = "Dữ liệu đánh giá không hợp lệ."; return RedirectToAction(nameof(OrderDetails), new { code }); }
        if (await _db.Reviews.AnyAsync(r => r.OrderId == order.Id && r.ProductId == model.ProductId)) { TempData["Error"] = "Bạn đã đánh giá sản phẩm này trong đơn."; return RedirectToAction(nameof(OrderDetails), new { code }); }

        _db.Reviews.Add(new Review { ProductId = model.ProductId, UserId = UserId, OrderId = order.Id, Rating = model.Rating, Comment = model.Comment?.Trim(), IsApproved = false });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn. Đánh giá sẽ hiển thị sau khi cửa hàng duyệt.";
        return RedirectToAction(nameof(OrderDetails), new { code });
    }

    // ---------- Yêu thích ----------
    [HttpGet("tai-khoan/yeu-thich")]
    public async Task<IActionResult> Wishlist()
    {
        var ids = await _db.Wishlists.Where(w => w.UserId == UserId).OrderByDescending(w => w.CreatedAt).Select(w => w.ProductId).ToListAsync();
        var cards = await _catalog.ToCardsAsync(_db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)), 200);
        return View(cards.OrderBy(c => ids.IndexOf(c.Id)).ToList());
    }

    [HttpPost("yeu-thich/toggle")]
    public async Task<IActionResult> ToggleWishlist(int productId)
    {
        var existing = await _db.Wishlists.FindAsync(UserId, productId);
        bool added;
        if (existing is null) { _db.Wishlists.Add(new Wishlist { UserId = UserId, ProductId = productId }); added = true; }
        else { _db.Wishlists.Remove(existing); added = false; }
        await _db.SaveChangesAsync();
        var count = await _db.Wishlists.CountAsync(w => w.UserId == UserId);
        return Json(new { success = true, added, count, message = added ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích." });
    }
}
