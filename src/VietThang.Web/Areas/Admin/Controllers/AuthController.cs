using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Account;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT01 – Đăng nhập trang quản trị (chỉ Admin, Employee). Tách riêng với đăng nhập khách hàng.</summary>
[Area("Admin")]
[AllowAnonymous]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet("Admin/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && (User.IsInRole(SeedData.RoleAdmin) || User.IsInRole(SeedData.RoleEmployee)))
            return RedirectToDashboard(returnUrl);
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost("Admin/login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(SeedData.RoleAdmin) && !roles.Contains(SeedData.RoleEmployee))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền truy cập trang quản trị.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) return RedirectToDashboard(returnUrl);

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Tài khoản bị tạm khóa 15 phút do đăng nhập sai nhiều lần."
            : "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpPost("Admin/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Hiện khi tài khoản đã đăng nhập nhưng không đủ vai trò (ví dụ nhân viên vào trang chỉ dành cho Admin).</summary>
    [HttpGet("Admin/access-denied")]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToDashboard(string? returnUrl)
        => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "Dashboard", new { area = "Admin" });
}
