using VietThang.Web.Models.Enums;

namespace VietThang.Web.Models.Entities;

/// <summary>Đơn hàng. Thông tin người nhận được lưu snapshot tại thời điểm đặt.</summary>
public class Order
{
    public int Id { get; set; }
    /// <summary>Mã đơn dạng VT + yyMMdd + STT, ví dụ VT240914001.</summary>
    public string OrderCode { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string? Note { get; set; }

    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public decimal TotalAmount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? CancelReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
}
