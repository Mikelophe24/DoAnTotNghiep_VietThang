using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Services;

namespace VietThang.Tests;

/// <summary>Tạo DbContext EF Core InMemory riêng cho từng test để các test không ảnh hưởng nhau.</summary>
public static class TestDb
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("vt-tests-" + Guid.NewGuid())
            // InMemory không hỗ trợ transaction thật; bỏ qua cảnh báo để test được service có BeginTransaction
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    public static ISettingService Settings(ApplicationDbContext db)
        => new SettingService(db, new MemoryCache(new MemoryCacheOptions()));

    /// <summary>Thêm cấu hình phí ship mặc định 30.000 đ và ngưỡng freeship 500.000 đ.</summary>
    public static void AddShippingSettings(ApplicationDbContext db, decimal fee = 30000, decimal threshold = 500000)
    {
        db.Settings.AddRange(
            new Setting { Key = SettingKeys.DefaultShippingFee, Value = fee.ToString("0") },
            new Setting { Key = SettingKeys.FreeShippingThreshold, Value = threshold.ToString("0") });
        db.SaveChanges();
    }
}
