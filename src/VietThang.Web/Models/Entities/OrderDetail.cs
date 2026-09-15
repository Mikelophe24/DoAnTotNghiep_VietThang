namespace VietThang.Web.Models.Entities;

/// <summary>Dòng chi tiết đơn hàng, lưu snapshot tên/giá tại thời điểm mua.</summary>
public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    /// <summary>Đơn giá sau khuyến mãi sản phẩm tại thời điểm mua.</summary>
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
