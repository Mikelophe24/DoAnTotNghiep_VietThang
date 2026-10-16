using System.Security.Cryptography;

namespace VietThang.Web.Services;

/// <summary>
/// Lấy khóa ký JWT mà không lưu trong mã nguồn. Thứ tự ưu tiên:
/// 1) cấu hình Jwt:Key (biến môi trường Jwt__Key hoặc dotnet user-secrets) – dùng khi triển khai;
/// 2) file App_Data/jwt-key.txt (tự sinh ngẫu nhiên lần chạy đầu, đã nằm trong .gitignore) – dùng khi phát triển.
/// </summary>
public static class JwtKeyProvider
{
    public static string Resolve(IConfiguration config, IHostEnvironment env)
    {
        var configured = config["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "jwt-key.txt");
        if (!File.Exists(file))
            File.WriteAllText(file, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
        return File.ReadAllText(file).Trim();
    }
}
