using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace VietThang.Web.Controllers.Api;

/// <summary>Controller gốc cho REST API dùng bởi Angular storefront. Không dùng anti-forgery (JWT bearer thay thế).</summary>
[ApiController]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>Lấy userId từ JWT nếu có (cho endpoint cho phép cả khách vãng lai).</summary>
    protected async Task<string?> TryGetJwtUserIdAsync()
    {
        var result = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (result.Succeeded && result.Principal is not null)
        {
            HttpContext.User = result.Principal;
            return result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        return null;
    }

    protected IActionResult ValidationError() =>
        BadRequest(new { message = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });
}
