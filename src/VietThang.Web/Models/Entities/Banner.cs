using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

public class Banner
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public BannerPosition Position { get; set; } = BannerPosition.HomeSlider;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
