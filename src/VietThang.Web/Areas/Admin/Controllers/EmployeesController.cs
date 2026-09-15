using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT09 – Nhân viên và phân quyền (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class EmployeesController : AdminBaseController
{
    private static readonly string[] StaffRoles = { SeedData.RoleAdmin, SeedData.RoleEmployee };
    private readonly UserManager<ApplicationUser> _userManager;

    public EmployeesController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index()
    {
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.RoleAdmin);
        var employees = await _userManager.GetUsersInRoleAsync(SeedData.RoleEmployee);
        var list = admins.Select(u => (User: u, Role: SeedData.RoleAdmin))
            .Concat(employees.Where(e => admins.All(a => a.Id != e.Id)).Select(u => (User: u, Role: SeedData.RoleEmployee)))
            .OrderBy(x => x.Role).ThenBy(x => x.User.FullName).ToList();
        ViewBag.CurrentUserId = _userManager.GetUserId(User);
        return View(list);
    }

    public IActionResult Create() => View("Form", new EmployeeFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "Nhập mật khẩu cho tài khoản mới.");
        if (!StaffRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        if (!ModelState.IsValid) return View("Form", model);

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true, FullName = model.FullName.Trim(), PhoneNumber = model.PhoneNumber, IsActive = model.IsActive };
        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Code is "DuplicateUserName" or "DuplicateEmail" ? "Email đã được sử dụng." : e.Description);
            return View("Form", model);
        }
        await _userManager.AddToRoleAsync(user, model.Role);
        TempData["Success"] = $"Đã tạo tài khoản {model.Email} ({model.Role}).";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return View("Form", new EmployeeFormViewModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email ?? "", PhoneNumber = user.PhoneNumber,
            Role = roles.Contains(SeedData.RoleAdmin) ? SeedData.RoleAdmin : SeedData.RoleEmployee, IsActive = user.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, EmployeeFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (!StaffRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Vai trò không hợp lệ.");
        var isSelf = user.Id == _userManager.GetUserId(User);
        if (isSelf && (model.Role != SeedData.RoleAdmin || !model.IsActive))
            ModelState.AddModelError(string.Empty, "Không thể tự hạ quyền hoặc khóa tài khoản đang đăng nhập.");
        if (!ModelState.IsValid) return View("Form", model);

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.IsActive = model.IsActive;
        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = model.Email;
            user.UserName = model.Email;
        }
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View("Form", model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var staffRolesOfUser = currentRoles.Where(r => StaffRoles.Contains(r)).ToList();
        if (!staffRolesOfUser.SequenceEqual(new[] { model.Role }))
        {
            await _userManager.RemoveFromRolesAsync(user, staffRolesOfUser);
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pw = await _userManager.ResetPasswordAsync(user, token, model.Password);
            if (!pw.Succeeded)
            {
                foreach (var e in pw.Errors) ModelState.AddModelError(nameof(model.Password), e.Description);
                return View("Form", model);
            }
        }
        if (!user.IsActive) await _userManager.UpdateSecurityStampAsync(user);
        TempData["Success"] = $"Đã cập nhật tài khoản {user.Email}.";
        return RedirectToAction(nameof(Index));
    }
}
