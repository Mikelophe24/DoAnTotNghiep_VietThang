using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;

namespace VietThang.Web.Controllers.Api;

/// <summary>
/// API giỏ hàng. Khách vãng lai giữ giỏ ở trình duyệt và gọi /quote để tính giá;
/// khách đăng nhập (JWT) dùng giỏ trong CSDL qua các endpoint còn lại.
/// </summary>
[Route("api/cart")]
public class CartApiController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;

    public CartApiController(ApplicationDbContext db, ICartService cart)
    {
        _db = db;
        _cart = cart;
    }

    /// <summary>Tính giá cho danh sách biến thể (giỏ khách vãng lai) kèm mã giảm giá, phí ship.</summary>
    [HttpPost("quote")]
    public async Task<IActionResult> Quote([FromBody] QuoteRequest req)
    {
        var lines = await _cart.BuildLinesAsync(req.Items.Select(i => (i.VariantId, i.Quantity)));
        return Ok(await _cart.BuildSummaryAsync(lines, req.CouponCode));
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Get([FromQuery] string? couponCode)
    {
        var lines = await _cart.GetLinesAsync();
        return Ok(await _cart.BuildSummaryAsync(lines, couponCode));
    }

    [HttpPost("items")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Add([FromBody] CartItemRequest req)
    {
        var (ok, message) = await _cart.AddAsync(req.VariantId, req.Quantity);
        if (!ok) return BadRequest(new { message });
        return Ok(new { message, count = await _cart.CountAsync() });
    }

    [HttpPut("items/{variantId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int variantId, [FromBody] CartItemRequest req)
    {
        await _cart.UpdateAsync(variantId, req.Quantity);
        return Ok(new { count = await _cart.CountAsync() });
    }

    [HttpDelete("items/{variantId:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Remove(int variantId)
    {
        await _cart.RemoveAsync(variantId);
        return Ok(new { count = await _cart.CountAsync() });
    }

    [HttpDelete]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync();
        return Ok(new { count = 0 });
    }

    /// <summary>Gộp giỏ ở trình duyệt vào giỏ CSDL sau khi đăng nhập.</summary>
    [HttpPost("merge")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Merge([FromBody] List<CartItemRequest> items)
    {
        var userId = CurrentUserId!;
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart is null) { cart = new Cart { UserId = userId }; _db.Carts.Add(cart); }
        foreach (var item in items.Where(i => i.Quantity > 0))
        {
            var existing = cart.Items.FirstOrDefault(i => i.VariantId == item.VariantId);
            if (existing is null) cart.Items.Add(new CartItem { VariantId = item.VariantId, Quantity = item.Quantity });
            else existing.Quantity += item.Quantity;
        }
        cart.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        var lines = await _cart.GetLinesAsync();
        return Ok(await _cart.BuildSummaryAsync(lines, null));
    }
}
