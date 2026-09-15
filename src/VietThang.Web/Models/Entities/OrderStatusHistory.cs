using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Lịch sử chuyển trạng thái đơn hàng.</summary>
public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public string? ChangedByUserId { get; set; }
    public ApplicationUser? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
}
