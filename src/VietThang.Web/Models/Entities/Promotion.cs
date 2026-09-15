using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Chương trình khuyến mãi áp lên sản phẩm (giảm % hoặc số tiền, có thời hạn).</summary>
public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();

    public bool IsEffectiveAt(DateTime time) => IsActive && StartDate <= time && time <= EndDate;
}

public class PromotionProduct
{
    public int PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
