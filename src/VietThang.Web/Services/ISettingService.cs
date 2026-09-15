using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VietThang.Web.Data;

namespace VietThang.Web.Services;

/// <summary>Đọc cấu hình hệ thống (bảng Settings) có cache 10 phút.</summary>
public interface ISettingService
{
    Task<string?> GetAsync(string key);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue);
    Task<int> GetIntAsync(string key, int defaultValue);
    void Invalidate();
}

public class SettingService : ISettingService
{
    private const string CacheKey = "settings.all";
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public SettingService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private async Task<Dictionary<string, string?>> LoadAsync()
    {
        var result = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await _db.Settings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
        });
        return result ?? new Dictionary<string, string?>();
    }

    public async Task<string?> GetAsync(string key) => (await LoadAsync()).GetValueOrDefault(key);

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue)
        => decimal.TryParse(await GetAsync(key), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : defaultValue;

    public async Task<int> GetIntAsync(string key, int defaultValue)
        => int.TryParse(await GetAsync(key), out var v) ? v : defaultValue;

    public void Invalidate() => _cache.Remove(CacheKey);
}
