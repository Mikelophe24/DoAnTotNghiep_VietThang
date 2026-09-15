namespace VietThang.Web.Services;

/// <summary>Lưu và xóa file tải lên (ảnh sản phẩm, banner, bài viết).</summary>
public interface IFileStorageService
{
    /// <summary>Lưu ảnh vào wwwroot/uploads/{folder}, trả về URL tương đối (/uploads/...).</summary>
    Task<string> SaveImageAsync(IFormFile file, string folder);
    void Delete(string? url);
}

public class LocalFileStorageService : IFileStorageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxSizeBytes = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file.Length == 0) throw new InvalidOperationException("File rỗng.");
        if (file.Length > MaxSizeBytes) throw new InvalidOperationException("Ảnh vượt quá 5MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("Chỉ nhận ảnh JPG, PNG, WEBP, GIF.");

        var safeFolder = string.Join("/", folder.Split('/', '\\').Where(p => !string.IsNullOrWhiteSpace(p) && p != ".."));
        var dir = Path.Combine(_env.WebRootPath, "uploads", safeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using var stream = File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/{safeFolder}/{fileName}";
    }

    public void Delete(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/")) return;
        var path = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(path)) File.Delete(path);
    }
}
