using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;

namespace VietThang.Web.Controllers.Api;

/// <summary>API đặt hàng (khách vãng lai hoặc đăng nhập) và tra cứu đơn.</summary>
[Route("api/checkout")]
public class CheckoutApiController : ApiControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly ISettingService _settings;

    public CheckoutApiController(ApplicationDbContext db, ICartService cart, IOrderService orders, ISettingService settings)
    {
        _db = db;
        _cart = cart;
        _orders = orders;
        _settings = settings;
    }

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] CheckoutRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var userId = await TryGetJwtUserIdAsync();

        List<CartLine> lines;
        if (userId is not null && (req.Items is null || req.Items.Count == 0))
            lines = await _cart.GetLinesAsync();
        else
            lines = await _cart.BuildLinesAsync((req.Items ?? new()).Select(i => (i.VariantId, i.Quantity)));

        var summary = await _cart.BuildSummaryAsync(lines, req.CouponCode);
        var result = await _orders.PlaceOrderFromSummaryAsync(req, userId, summary);
        if (!result.Success) return BadRequest(new { message = result.Error });

        if (userId is not null)
        {
            await _db.CartItems.Where(i => i.Cart.UserId == userId).ExecuteDeleteAsync();
            if (req.SaveAddress)
            {
                var exists = await _db.Addresses.AnyAsync(a => a.UserId == userId && a.Street == req.Street && a.Ward == req.Ward && a.District == req.District);
                if (!exists)
                {
                    var hasDefault = await _db.Addresses.AnyAsync(a => a.UserId == userId && a.IsDefault);
                    _db.Addresses.Add(new Address { UserId = userId, ReceiverName = req.CustomerName, Phone = req.Phone, Province = req.Province, District = req.District, Ward = req.Ward, Street = req.Street, IsDefault = !hasDefault });
                    await _db.SaveChangesAsync();
                }
            }
        }

        var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.OrderCode == result.OrderCode);
        return Ok(new
        {
            orderCode = order.OrderCode,
            order.TotalAmount,
            paymentMethod = order.PaymentMethod,
            bankAccount = order.PaymentMethod == PaymentMethod.BankTransfer ? await _settings.GetAsync(SettingKeys.BankAccount) : null
        });
    }

    [HttpGet("orders/{code}")]
    public async Task<IActionResult> Lookup(string code, [FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return BadRequest(new { message = "Nhập số điện thoại đặt hàng." });
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details).Include(o => o.StatusHistories.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.OrderCode == code.Trim().ToUpper() && o.Phone == phone.Trim());
        if (order is null) return NotFound(new { message = "Không tìm thấy đơn hàng với mã và số điện thoại này." });
        return Ok(OrderDto.From(order));
    }
}

/// <summary>DTO đơn hàng dùng chung cho API tra cứu và tài khoản.</summary>
public static class OrderDto
{
    public static object From(Order o) => new
    {
        o.Id, o.OrderCode, o.CustomerName, o.Phone, o.Email, o.ShippingAddress, o.Province, o.District, o.Ward, o.Note,
        o.SubTotal, o.ShippingFee, o.DiscountAmount, o.TotalAmount,
        paymentMethod = o.PaymentMethod, paymentMethodName = o.PaymentMethod.ToDisplay(),
        paymentStatus = o.PaymentStatus, paymentStatusName = o.PaymentStatus.ToDisplay(),
        status = o.Status, statusName = o.Status.ToDisplay(),
        o.CancelReason, o.CreatedAt, o.CompletedAt,
        canCancel = o.Status == OrderStatus.Pending,
        details = o.Details.Select(d => new { d.Id, d.VariantId, d.ProductName, d.Sku, d.ColorName, d.SizeName, d.UnitPrice, d.Quantity, d.LineTotal }),
        histories = o.StatusHistories.Select(h => new { status = h.ToStatus, statusName = h.ToStatus.ToDisplay(), h.Note, h.ChangedAt })
    };
}
