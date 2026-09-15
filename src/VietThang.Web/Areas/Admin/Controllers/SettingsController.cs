using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT21 – Cấu hình hệ thống (chỉ Admin).</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class SettingsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;

    public SettingsController(ApplicationDbContext db, ISettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IActionResult> Index()
    {
        return View(new SettingsFormViewModel
        {
            StoreName = await _settings.GetAsync(SettingKeys.StoreName) ?? "",
            Hotline = await _settings.GetAsync(SettingKeys.Hotline) ?? "",
            Email = await _settings.GetAsync(SettingKeys.Email),
            DefaultShippingFee = await _settings.GetDecimalAsync(SettingKeys.DefaultShippingFee, 30000),
            FreeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000),
            LowStockThreshold = await _settings.GetIntAsync(SettingKeys.LowStockThreshold, 5),
            BankAccount = await _settings.GetAsync(SettingKeys.BankAccount)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(SettingsFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var values = new Dictionary<string, string?>
        {
            [SettingKeys.StoreName] = model.StoreName.Trim(),
            [SettingKeys.Hotline] = model.Hotline.Trim(),
            [SettingKeys.Email] = model.Email?.Trim(),
            [SettingKeys.DefaultShippingFee] = model.DefaultShippingFee.ToString(CultureInfo.InvariantCulture),
            [SettingKeys.FreeShippingThreshold] = model.FreeShippingThreshold.ToString(CultureInfo.InvariantCulture),
            [SettingKeys.LowStockThreshold] = model.LowStockThreshold.ToString(CultureInfo.InvariantCulture),
            [SettingKeys.BankAccount] = model.BankAccount?.Trim()
        };
        var existing = await _db.Settings.ToDictionaryAsync(s => s.Key);
        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var s)) s.Value = value;
            else _db.Settings.Add(new Setting { Key = key, Value = value });
        }
        await _db.SaveChangesAsync();
        _settings.Invalidate();
        TempData["Success"] = "Đã lưu cấu hình.";
        return RedirectToAction(nameof(Index));
    }
}
