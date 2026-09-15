using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Controllers;

/// <summary>KH07 – Đặt hàng.</summary>
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly ISettingService _settings;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(ApplicationDbContext db, ICartService cart, IOrderService orders,
        ISettingService settings, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cart = cart;
        _orders = orders;
        _settings = settings;
        _userManager = userManager;
    }

    [HttpGet("thanh-toan")]
    public async Task<IActionResult> Index()
    {
        var summary = await _cart.GetSummaryAsync();
        if (summary.IsEmpty)
        {
            TempData["Error"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction("Index", "Cart");
        }
        if (summary.Lines.Any(l => !l.IsAvailable))
        {
            TempData["Error"] = "Một số sản phẩm trong giỏ không đủ hàng, vui lòng cập nhật số lượng.";
            return RedirectToAction("Index", "Cart");
        }

        var vm = new CheckoutViewModel();
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            vm.CustomerName = user.FullName;
            vm.Phone = user.PhoneNumber ?? string.Empty;
            vm.Email = user.Email;
            vm.SavedAddresses = await _db.Addresses.AsNoTracking()
                .Where(a => a.UserId == user.Id).OrderByDescending(a => a.IsDefault).ToListAsync();
            var def = vm.SavedAddresses.FirstOrDefault();
            if (def is not null)
            {
                vm.CustomerName = def.ReceiverName;
                vm.Phone = def.Phone;
                vm.Province = def.Province;
                vm.District = def.District;
                vm.Ward = def.Ward;
                vm.Street = def.Street;
            }
        }
        await FillDisplayAsync(vm, summary);
        return View(vm);
    }

    [HttpPost("thanh-toan")]
    public async Task<IActionResult> Index(CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (!ModelState.IsValid)
        {
            await FillDisplayAsync(model, await _cart.GetSummaryAsync(), user);
            return View(model);
        }

        var result = await _orders.PlaceOrderAsync(model, user?.Id);
        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            await FillDisplayAsync(model, await _cart.GetSummaryAsync(), user);
            return View(model);
        }

        if (user is not null && model.SaveAddress)
        {
            var exists = await _db.Addresses.AnyAsync(a => a.UserId == user.Id && a.Street == model.Street && a.Ward == model.Ward && a.District == model.District);
            if (!exists)
            {
                var hasDefault = await _db.Addresses.AnyAsync(a => a.UserId == user.Id && a.IsDefault);
                _db.Addresses.Add(new Address
                {
                    UserId = user.Id, ReceiverName = model.CustomerName, Phone = model.Phone,
                    Province = model.Province, District = model.District, Ward = model.Ward, Street = model.Street,
                    IsDefault = !hasDefault
                });
                await _db.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Success), new { code = result.OrderCode });
    }

    [HttpGet("thanh-toan/thanh-cong/{code}")]
    public async Task<IActionResult> Success(string code)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.OrderCode == code);
        if (order is null) return NotFound();
        ViewBag.BankAccount = order.PaymentMethod == PaymentMethod.BankTransfer ? await _settings.GetAsync(SettingKeys.BankAccount) : null;
        return View(order);
    }

    private async Task FillDisplayAsync(CheckoutViewModel vm, CartSummary summary, ApplicationUser? user = null)
    {
        vm.Cart = summary;
        vm.BankAccount = await _settings.GetAsync(SettingKeys.BankAccount);
        if (user is not null && vm.SavedAddresses.Count == 0)
            vm.SavedAddresses = await _db.Addresses.AsNoTracking().Where(a => a.UserId == user.Id).OrderByDescending(a => a.IsDefault).ToListAsync();
    }
}
