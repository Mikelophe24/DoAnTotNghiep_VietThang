namespace VietThang.Web.Models.Entities;

/// <summary>Ảnh sản phẩm, có thể gắn với một màu cụ thể.</summary>
public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ColorId { get; set; }
    public Color? Color { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }
}
