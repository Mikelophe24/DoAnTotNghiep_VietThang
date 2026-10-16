using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Api;

namespace VietThang.Web.Controllers.Api;

/// <summary>API đăng ký / đăng nhập bằng JWT và quên mật khẩu cho Angular storefront (KH09).</summary>
[Route("api/auth")]
public class AuthApiController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly IAppEmailSender _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthApiController> _logger;

    public AuthApiController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwt, IAppEmailSender email, IConfiguration config, ILogger<AuthApiController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _email = email;
        _config = config;
        _logger = logger;
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
        if (!result.Succeeded) return BadRequest(new { message = TranslateErrors(result) });
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

    /// <summary>
    /// Gửi email chứa liên kết đặt lại mật khẩu (hiệu lực theo token của Identity, mặc định 1 ngày).
    /// Luôn trả về cùng một thông báo để không lộ email nào đã đăng ký.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = (_config["Storefront:Url"] ?? "http://localhost:4200").TrimEnd('/');
            var link = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encoded}";
            try
            {
                await _email.SendAsync(user.Email, "[Việt Thắng] Đặt lại mật khẩu",
                    $"""
                    <p>Chào {user.FullName},</p>
                    <p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản tại Thời trang Việt Thắng.</p>
                    <p><a href="{link}">Bấm vào đây để đặt mật khẩu mới</a></p>
                    <p>Hoặc sao chép liên kết: {link}</p>
                    <p>Liên kết có hiệu lực trong 24 giờ. Nếu không phải bạn yêu cầu, hãy bỏ qua email này.</p>
                    """);
            }
            catch (Exception ex) { _logger.LogError(ex, "Không gửi được email đặt lại mật khẩu tới {Email}", user.Email); }
        }
        return Ok(new { message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (!ModelState.IsValid) return ValidationError();
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user is null) return BadRequest(new { message = "Liên kết không hợp lệ hoặc đã hết hạn." });

        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(req.Token)); }
        catch (FormatException) { return BadRequest(new { message = "Liên kết không hợp lệ hoặc đã hết hạn." }); }

        var result = await _userManager.ResetPasswordAsync(user, token, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors.Any(e => e.Code == "InvalidToken") ? "Liên kết không hợp lệ hoặc đã hết hạn." : TranslateErrors(result) });

        // Mở khóa nếu tài khoản đang bị khóa tạm do nhập sai mật khẩu nhiều lần
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        return Ok(new { message = "Đã đặt lại mật khẩu. Bạn có thể đăng nhập bằng mật khẩu mới." });
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwt.CreateToken(user, roles);
        return new AuthResponse(token, expires, new UserInfo(user.Id, user.FullName, user.Email, user.PhoneNumber, roles));
    }

    private static string TranslateErrors(IdentityResult result) => string.Join(" ", result.Errors.Select(e => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Email này đã được đăng ký.",
        "PasswordTooShort" => "Mật khẩu tối thiểu 8 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất một chữ số.",
        "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất một chữ in hoa.",
        "PasswordRequiresLower" => "Mật khẩu phải có ít nhất một chữ thường.",
        _ => e.Description
    }).Distinct());
}
