using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Data;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>Controller gốc cho khu vực quản trị: yêu cầu vai trò Admin hoặc Employee.</summary>
[Area("Admin")]
[Authorize(Roles = $"{SeedData.RoleAdmin},{SeedData.RoleEmployee}")]
public abstract class AdminBaseController : Controller
{
}
