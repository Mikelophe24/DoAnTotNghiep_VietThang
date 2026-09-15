using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Phiếu nhập hàng từ nhà cung cấp. Khi duyệt (Completed) sẽ cộng tồn kho.</summary>
public class GoodsReceipt
{
    public int Id { get; set; }
    /// <summary>Mã phiếu dạng PN + yyMMdd + STT.</summary>
    public string Code { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public DateTime ReceiptDate { get; set; } = DateTime.Now;
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    public ICollection<GoodsReceiptDetail> Details { get; set; } = new List<GoodsReceiptDetail>();
}

public class GoodsReceiptDetail
{
    public int Id { get; set; }
    public int GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public decimal LineTotal => UnitCost * Quantity;
}
