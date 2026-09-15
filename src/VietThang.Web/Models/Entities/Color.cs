namespace VietThang.Web.Models.Entities;

public class Color
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Mã màu hiển thị, dạng #RRGGBB.</summary>
    public string? HexCode { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
