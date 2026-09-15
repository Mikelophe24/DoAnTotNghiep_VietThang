namespace VietThang.Web.Models.Entities;

/// <summary>Điểm bán trong hệ thống cửa hàng.</summary>
public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? OpeningHours { get; set; }
    public string? MapEmbedUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
