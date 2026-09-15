using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers.Api;

/// <summary>API tài khoản khách hàng (JWT): hồ sơ, mật khẩu, địa chỉ, đơn hàng, đánh giá, yêu thích.</summary>
[Route("api/account")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AccountApiController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrderService _orders;
    private readonly ICatalogService _catalog;

    public AccountApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IOrderService orders, ICatalogService catalog)
    {
        _db = db;
        _userManager = userManager;
        _orders = orders;
        _catalog = catalog;
    }

    private string UserId => CurrentUserId!;

    // ---------- Hồ sơ ----------
    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        var u = await _userManager.FindByIdAsync(UserId);
        if (u is null) return NotFound();
        return Ok(new { u.FullName, u.Email, PhoneNumber = u.PhoneNumber, u.Gender, u.DateOfBirth, u.CreatedAt });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileViewModel model)
    {
        if (!ModelState.IsValid) return ValidationError();
        var u = await _userManager.FindByIdAsync(UserId);
        if (u is null) return NotFound();
        u.FullName = model.FullName.Trim();
        u.PhoneNumber = model.PhoneNumber.Trim();
        u.Gender = model.Gender;
        u.DateOfBirth = model.DateOfBirth;
        await _userManager.UpdateAsync(u);
        return Ok(new { message = "Đã cập nhật hồ sơ." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return ValidationError();
        var u = await _userManager.FindByIdAsync(UserId);
        if (u is null) return NotFound();
        var result = await _userManager.ChangePasswordAsync(u, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors.Any(e => e.Code == "PasswordMismatch") ? "Mật khẩu hiện tại không đúng." : string.Join(" ", result.Errors.Select(e => e.Description)) });
        return Ok(new { message = "Đã đổi mật khẩu." });
    }

    // ---------- Địa chỉ ----------
    [HttpGet("addresses")]
    public async Task<IActionResult> Addresses()
        => Ok(await _db.Addresses.AsNoTracking().Where(a => a.UserId == UserId).OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id)
            .Select(a => new { a.Id, a.ReceiverName, a.Phone, a.Province, a.District, a.Ward, a.Street, a.IsDefault }).ToListAsync());

    [HttpPost("addresses")]
    public Task<IActionResult> CreateAddress([FromBody] AddressFormViewModel model) => SaveAddressAsync(model, null);

    [HttpPut("addresses/{id:int}")]
    public Task<IActionResult> UpdateAddress(int id, [FromBody] AddressFormViewModel model) => SaveAddressAsync(model, id);

    private async Task<IActionResult> SaveAddressAsync(AddressFormViewModel model, int? id)
    {
        if (!ModelState.IsValid) return ValidationError();
        Address address;
        if (id.HasValue)
        {
            address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId) ?? throw new KeyNotFoundException();
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
        var hasOther = await _db.Addresses.AnyAsync(a => a.UserId == UserId && a.Id != id);
        address.IsDefault = model.IsDefault || !hasOther;
        if (address.IsDefault)
            await _db.Addresses.Where(a => a.UserId == UserId && a.Id != id).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
        await _db.SaveChangesAsync();
        return Ok(new { address.Id, message = "Đã lưu địa chỉ." });
    }

    [HttpDelete("addresses/{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        await _db.Addresses.Where(a => a.Id == id && a.UserId == UserId).ExecuteDeleteAsync();
        return Ok(new { message = "Đã xóa địa chỉ." });
    }

    [HttpPost("addresses/{id:int}/default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        await _db.Addresses.Where(a => a.UserId == UserId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, a => a.Id == id));
        return Ok(new { message = "Đã đặt địa chỉ mặc định." });
    }

    // ---------- Đơn hàng ----------
    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] int page = 1)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Details).Where(o => o.UserId == UserId).OrderByDescending(o => o.CreatedAt);
        var paged = await Helpers.PagedList<Order>.CreateAsync(query, page, 10);
        return Ok(new
        {
            items = paged.Items.Select(o => new { o.Id, o.OrderCode, o.CreatedAt, o.TotalAmount, status = o.Status, statusName = Helpers.EnumExtensions.ToDisplay(o.Status), itemCount = o.Details.Sum(d => d.Quantity) }),
            paged.PageIndex, paged.TotalPages, paged.TotalCount
        });
    }

    [HttpGet("orders/{code}")]
    public async Task<IActionResult> OrderDetails(string code)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details).Include(o => o.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        var reviewed = await _db.Reviews.Where(r => r.OrderId == order.Id).Select(r => r.ProductId).ToListAsync();
        var variantProduct = await _db.ProductVariants.Where(v => order.Details.Select(d => d.VariantId).Contains(v.Id))
            .Select(v => new { v.Id, v.ProductId, v.Product.Slug }).ToListAsync();
        return Ok(new
        {
            order = OrderDto.From(order),
            products = variantProduct.Select(v => new { variantId = v.Id, productId = v.ProductId, slug = v.Slug, reviewed = reviewed.Contains(v.ProductId) })
        });
    }

    [HttpPost("orders/{code}/cancel")]
    public async Task<IActionResult> CancelOrder(string code, [FromBody] CancelOrderRequest? req)
    {
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (order.Status != OrderStatus.Pending) return BadRequest(new { message = "Chỉ hủy được đơn đang chờ xác nhận. Vui lòng gọi hotline để được hỗ trợ." });
        var (ok, message) = await _orders.ChangeStatusAsync(order.Id, OrderStatus.Cancelled, "Khách tự hủy" + (string.IsNullOrWhiteSpace(req?.Reason) ? "" : ": " + req.Reason.Trim()), UserId);
        return ok ? Ok(new { message = "Đã hủy đơn hàng." }) : BadRequest(new { message });
    }

    [HttpPost("orders/{code}/reviews")]
    public async Task<IActionResult> Review(string code, [FromBody] ReviewRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details).FirstOrDefaultAsync(o => o.OrderCode == code && o.UserId == UserId);
        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng." });
        if (order.Status != OrderStatus.Completed) return BadRequest(new { message = "Chỉ đánh giá được khi đơn đã hoàn thành." });
        var inOrder = await _db.ProductVariants.AnyAsync(v => v.ProductId == req.ProductId && order.Details.Select(d => d.VariantId).Contains(v.Id));
        if (!inOrder) return BadRequest(new { message = "Sản phẩm không thuộc đơn hàng này." });
        if (await _db.Reviews.AnyAsync(r => r.OrderId == order.Id && r.ProductId == req.ProductId)) return BadRequest(new { message = "Bạn đã đánh giá sản phẩm này trong đơn." });
        _db.Reviews.Add(new Review { ProductId = req.ProductId, UserId = UserId, OrderId = order.Id, Rating = req.Rating, Comment = req.Comment?.Trim(), IsApproved = false });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cảm ơn bạn. Đánh giá sẽ hiển thị sau khi cửa hàng duyệt." });
    }

    // ---------- Yêu thích ----------
    [HttpGet("wishlist")]
    public async Task<IActionResult> Wishlist()
    {
        var ids = await _db.Wishlists.Where(w => w.UserId == UserId).OrderByDescending(w => w.CreatedAt).Select(w => w.ProductId).ToListAsync();
        var cards = await _catalog.ToCardsAsync(_db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)), 200);
        return Ok(cards.OrderBy(c => ids.IndexOf(c.Id)));
    }

    [HttpPost("wishlist/{productId:int}/toggle")]
    public async Task<IActionResult> ToggleWishlist(int productId)
    {
        var existing = await _db.Wishlists.FindAsync(UserId, productId);
        bool added;
        if (existing is null) { _db.Wishlists.Add(new Wishlist { UserId = UserId, ProductId = productId }); added = true; }
        else { _db.Wishlists.Remove(existing); added = false; }
        await _db.SaveChangesAsync();
        return Ok(new { added, message = added ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích." });
    }
}
