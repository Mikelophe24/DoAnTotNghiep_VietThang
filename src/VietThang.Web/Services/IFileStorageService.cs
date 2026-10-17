using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace VietThang.Web.Services;

/// <summary>Lưu và xóa file tải lên (ảnh sản phẩm, danh mục, banner, bài viết).</summary>
public interface IFileStorageService
{
    /// <summary>Lưu ảnh vào nơi lưu trữ, trả về URL dùng được trong thẻ img (tương đối /uploads/... hoặc tuyệt đối https://...).</summary>
    Task<string> SaveImageAsync(IFormFile file, string folder);
    void Delete(string? url);
}

/// <summary>Kiểm tra chung cho mọi nơi lưu trữ: định dạng và kích thước ảnh.</summary>
public static class ImageUploadRules
{
    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>Ném InvalidOperationException nếu file không hợp lệ; trả về phần mở rộng chữ thường.</summary>
    public static string Validate(IFormFile file)
    {
        if (file.Length == 0) throw new InvalidOperationException("File rỗng.");
        if (file.Length > MaxSizeBytes) throw new InvalidOperationException("Ảnh vượt quá 5MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("Chỉ nhận ảnh JPG, PNG, WEBP, GIF.");
        return ext;
    }

    /// <summary>Bỏ các đoạn rỗng và ".." để không thoát ra ngoài thư mục gốc.</summary>
    public static string SafeFolder(string folder)
        => string.Join("/", folder.Split('/', '\\').Where(p => !string.IsNullOrWhiteSpace(p) && p != ".."));
}

/// <summary>Lưu ảnh vào wwwroot/uploads/{folder} trên đĩa của máy chạy ứng dụng (mặc định khi chưa cấu hình Cloudinary).</summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        var ext = ImageUploadRules.Validate(file);
        var safeFolder = ImageUploadRules.SafeFolder(folder);
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

/// <summary>Cấu hình mục "Cloudinary" trong appsettings / user-secrets.</summary>
public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    /// <summary>Thư mục gốc trên Cloudinary, mặc định "vietthang".</summary>
    public string Folder { get; set; } = "vietthang";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(ApiSecret);
}

/// <summary>
/// Lưu ảnh lên Cloudinary (bật khi có đủ Cloudinary:CloudName/ApiKey/ApiSecret). URL trả về là tuyệt đối (https://res.cloudinary.com/...),
/// nên ảnh hiển thị được từ mọi máy và không mất khi deploy lại. Ảnh cũ dạng /uploads/... vẫn được giữ nguyên trên đĩa.
/// </summary>
public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryFileStorageService> _logger;

    public CloudinaryFileStorageService(CloudinaryOptions options, ILogger<CloudinaryFileStorageService> logger)
    {
        _options = options;
        _logger = logger;
        _cloudinary = new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret)) { Api = { Secure = true } };
    }

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        ImageUploadRules.Validate(file);
        var safeFolder = ImageUploadRules.SafeFolder(folder);

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            // Thư mục: vietthang/products/12 ; tên file do Cloudinary sinh ngẫu nhiên để không trùng
            Folder = string.IsNullOrWhiteSpace(safeFolder) ? _options.Folder : $"{_options.Folder}/{safeFolder}",
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error is not null || result.SecureUrl is null)
            throw new InvalidOperationException("Không tải được ảnh lên Cloudinary: " + (result.Error?.Message ?? "không rõ lỗi"));

        return result.SecureUrl.ToString();
    }

    public void Delete(string? url)
    {
        var publicId = ExtractPublicId(url);
        if (publicId is null) return;
        try
        {
            // Gọi đồng bộ vì interface Delete không async; thao tác xóa ảnh không chặn nghiệp vụ chính
            var result = _cloudinary.Destroy(new DeletionParams(publicId));
            if (result.Result != "ok" && result.Result != "not found")
                _logger.LogWarning("Xóa ảnh Cloudinary {PublicId} trả về {Result}", publicId, result.Result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không xóa được ảnh Cloudinary {PublicId}", publicId);
        }
    }

    /// <summary>
    /// Lấy public_id từ URL Cloudinary dạng https://res.cloudinary.com/{cloud}/image/upload/[biến đổi/]v123/{folder}/{tên}.{ext}.
    /// Trả về null nếu không phải URL Cloudinary (ví dụ ảnh cũ /uploads/...).
    /// </summary>
    public static string? ExtractPublicId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("res.cloudinary.com/", StringComparison.OrdinalIgnoreCase)) return null;

        const string marker = "/upload/";
        var idx = url.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;

        var rest = url[(idx + marker.Length)..];
        var q = rest.IndexOfAny(new[] { '?', '#' });
        if (q >= 0) rest = rest[..q];

        // Các đoạn biến đổi (w_300,c_fill) nếu có đứng trước đoạn phiên bản (v1712345678); public_id là phần sau phiên bản
        var segments = rest.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        var versionIndex = segments.FindIndex(s => s.Length > 1 && s[0] == 'v' && s[1..].All(char.IsDigit));
        if (versionIndex >= 0) segments = segments.Skip(versionIndex + 1).ToList();
        if (segments.Count == 0) return null;

        var last = segments[^1];
        var dot = last.LastIndexOf('.');
        if (dot > 0) segments[^1] = last[..dot];
        return string.Join('/', segments);
    }
}
