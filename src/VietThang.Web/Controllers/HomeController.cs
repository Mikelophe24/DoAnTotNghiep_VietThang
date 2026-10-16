using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Models;

namespace VietThang.Web.Controllers;

/// <summary>
/// Backend chỉ còn REST API (/api/*) và trang quản trị (/Admin). Giao diện khách hàng là ứng dụng Angular,
/// nên truy cập gốc "/" được chuyển sang địa chỉ storefront cấu hình ở Storefront:Url.
/// </summary>
public class HomeController : Controller
{
    private readonly IConfiguration _config;
    public HomeController(IConfiguration config) => _config = config;

    public IActionResult Index() => Redirect(_config["Storefront:Url"] ?? "/Admin");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
