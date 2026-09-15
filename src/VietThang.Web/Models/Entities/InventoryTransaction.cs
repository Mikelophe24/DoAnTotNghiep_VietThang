using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Nhật ký biến động tồn kho theo biến thể. Quantity dương là tăng, âm là giảm.</summary>
public class InventoryTransaction
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public InventoryType Type { get; set; }
    public int Quantity { get; set; }
    public int StockAfter { get; set; }
    /// <summary>"GoodsReceipt" | "Order" | "Adjustment"</summary>
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
