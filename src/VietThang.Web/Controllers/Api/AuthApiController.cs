using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;

namespace VietThang.Web.Controllers.Api;

/// <summary>API đăng ký / đăng nhập bằng JWT cho Angular storefront.</summary>
[Route("api/auth")]
public class AuthApiController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwt;

    public AuthApiController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IJwtTokenService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (result.IsLockedOut) return Unauthorized(new { message = "Tài khoản bị tạm khóa 15 phút do đăng nhập sai nhiều lần." });
        if (!result.Succeeded) return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var user = new ApplicationUser { UserName = req.Email, Email = req.Email, FullName = req.FullName.Trim(), PhoneNumber = req.PhoneNumber, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join(" ", result.Errors.Select(e => e.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" => "Email này đã được đăng ký.",
                "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
                "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ in hoa.",
                "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ thường.",
                _ => e.Description
            }));
            return BadRequest(new { message = msg });
        }
        await _userManager.AddToRoleAsync(user, SeedData.RoleCustomer);
        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId!);
        if (user is null || !user.IsActive) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfo(user.Id, user.FullName, user.Email, user.PhoneNumber, roles));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwt.CreateToken(user, roles);
        return new AuthResponse(token, expires, new UserInfo(user.Id, user.FullName, user.Email, user.PhoneNumber, roles));
    }
}
