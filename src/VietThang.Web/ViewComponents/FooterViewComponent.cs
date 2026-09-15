using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;

namespace VietThang.Web.ViewComponents;

public class FooterViewModel
{
    public string StoreName { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal FreeShippingThreshold { get; set; }
    public List<Store> Stores { get; set; } = new();
}

public class FooterViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;

    public FooterViewComponent(ApplicationDbContext db, ISettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View(new FooterViewModel
        {
            StoreName = await _settings.GetAsync(SettingKeys.StoreName) ?? "Thời trang Việt Thắng",
            Hotline = await _settings.GetAsync(SettingKeys.Hotline) ?? string.Empty,
            Email = await _settings.GetAsync(SettingKeys.Email) ?? string.Empty,
            FreeShippingThreshold = await _settings.GetDecimalAsync(SettingKeys.FreeShippingThreshold, 500000),
            Stores = await _db.Stores.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).Take(4).ToListAsync()
        });
    }
}
