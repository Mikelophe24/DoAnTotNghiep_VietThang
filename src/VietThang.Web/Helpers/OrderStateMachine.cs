using VietThang.Web.Models.Enums;

namespace VietThang.Web.Helpers;

/// <summary>Quy tắc chuyển trạng thái đơn hàng (mục 4.4 tài liệu CSDL).</summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Shipping, OrderStatus.Cancelled },
        [OrderStatus.Shipping] = new[] { OrderStatus.Completed },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) => Transitions[from].Contains(to);
    public static IReadOnlyList<OrderStatus> NextStatuses(OrderStatus from) => Transitions[from];
    public static bool IsFinal(OrderStatus s) => Transitions[s].Length == 0;

    /// <summary>Nhãn nút hành động cho nhân viên.</summary>
    public static string ActionLabel(OrderStatus to) => to switch
    {
        OrderStatus.Confirmed => "Xác nhận đơn",
        OrderStatus.Shipping => "Bắt đầu giao hàng",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Hủy đơn",
        _ => to.ToDisplay()
    };
}
