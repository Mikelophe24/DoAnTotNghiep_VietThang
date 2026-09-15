using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Services;

namespace VietThang.Web.Controllers;

/// <summary>KH05, KH06 – Giỏ hàng và mã giảm giá.</summary>
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly IPromotionService _promotions;

    public CartController(ICartService cart, IPromotionService promotions)
    {
        _cart = cart;
        _promotions = promotions;
    }

    [HttpGet("gio-hang")]
    public async Task<IActionResult> Index() => View(await _cart.GetSummaryAsync());

    /// <summary>Thêm vào giỏ bằng AJAX, trả JSON để cập nhật badge.</summary>
    [HttpPost("gio-hang/them")]
    public async Task<IActionResult> Add(int variantId, int quantity = 1)
    {
        var (ok, message) = await _cart.AddAsync(variantId, quantity);
        var count = await _cart.CountAsync();
        if (Request.Headers.XRequestedWith == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            return Json(new { success = ok, message, count });

        TempData[ok ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("gio-hang/cap-nhat")]
    public async Task<IActionResult> Update(int variantId, int quantity)
    {
        await _cart.UpdateAsync(variantId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("gio-hang/xoa")]
    public async Task<IActionResult> Remove(int variantId)
    {
        await _cart.RemoveAsync(variantId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("gio-hang/ma-giam-gia")]
    public async Task<IActionResult> ApplyCoupon(string? code)
    {
        var lines = await _cart.GetLinesAsync();
        var subTotal = lines.Sum(l => l.LineTotal);
        var result = await _promotions.ValidateCouponAsync(code, subTotal);
        if (result.Success)
        {
            _cart.SetCouponCode(result.Coupon!.Code);
            TempData["Success"] = $"Đã áp dụng mã {result.Coupon.Code}, giảm {result.Discount:N0} đ.";
        }
        else
        {
            _cart.SetCouponCode(null);
            TempData["Error"] = result.Error ?? "Vui lòng nhập mã giảm giá.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("gio-hang/bo-ma-giam-gia")]
    public IActionResult RemoveCoupon()
    {
        _cart.SetCouponCode(null);
        return RedirectToAction(nameof(Index));
    }
}
