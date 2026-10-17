using VietThang.Web.Services;

namespace VietThang.Tests;

public class FileStorageTests
{
    [Theory]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1712345678/vietthang/products/12/abc123.jpg", "vietthang/products/12/abc123")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/v1712345678/vietthang/banners/xyz.png", "vietthang/banners/xyz")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/w_300,c_fill/v1712345678/vietthang/posts/t.webp", "vietthang/posts/t")]
    [InlineData("https://res.cloudinary.com/demo/image/upload/vietthang/categories/k.jpg?x=1", "vietthang/categories/k")]
    public void ExtractPublicId_UrlCloudinary_LayDungPublicId(string url, string expected)
        => Assert.Equal(expected, CloudinaryFileStorageService.ExtractPublicId(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/uploads/products/7/abc.jpg")]
    [InlineData("/images/products/VT-BL-011-BE.svg")]
    [InlineData("https://example.com/image/upload/v1/x.jpg")]
    public void ExtractPublicId_KhongPhaiCloudinary_TraVeNull(string? url)
        => Assert.Null(CloudinaryFileStorageService.ExtractPublicId(url));

    [Fact]
    public void SafeFolder_LoaiBoDauChamChamVaDoanRong()
    {
        Assert.Equal("products/12", ImageUploadRules.SafeFolder("products//12"));
        Assert.Equal("products/12", ImageUploadRules.SafeFolder("../products/../12"));
        Assert.Equal("banners", ImageUploadRules.SafeFolder("banners\\"));
    }

    [Fact]
    public void CloudinaryOptions_ThieuBatKyKhoaNao_KhongDuocCoiLaDaCauHinh()
    {
        Assert.False(new CloudinaryOptions().IsConfigured);
        Assert.False(new CloudinaryOptions { CloudName = "a", ApiKey = "b" }.IsConfigured);
        Assert.True(new CloudinaryOptions { CloudName = "a", ApiKey = "b", ApiSecret = "c" }.IsConfigured);
    }
}
