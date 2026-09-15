using VietThang.Web.Models.Enums;

namespace VietThang.Web.Helpers;

/// <summary>Tên hiển thị tiếng Việt và lớp CSS cho các enum.</summary>
public static class EnumExtensions
{
    public static string ToDisplay(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Shipping => "Đang giao hàng",
        OrderStatus.Completed => "Hoàn thành",
        OrderStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static string ToBadgeClass(this OrderStatus s) => s switch
    {
        OrderStatus.Pending => "bg-warning text-dark",
        OrderStatus.Confirmed => "bg-info text-dark",
        OrderStatus.Shipping => "bg-primary",
        OrderStatus.Completed => "bg-success",
        OrderStatus.Cancelled => "bg-secondary",
        _ => "bg-light text-dark"
    };

    public static string ToDisplay(this PaymentMethod m) => m switch
    {
        PaymentMethod.Cod => "Thanh toán khi nhận hàng (COD)",
        PaymentMethod.BankTransfer => "Chuyển khoản ngân hàng",
        _ => m.ToString()
    };

    public static string ToDisplay(this PaymentStatus p) => p switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Paid => "Đã thanh toán",
        PaymentStatus.Refunded => "Đã hoàn tiền",
        _ => p.ToString()
    };

    public static string ToDisplay(this ReceiptStatus s) => s switch
    {
        ReceiptStatus.Draft => "Nháp",
        ReceiptStatus.Completed => "Đã nhập kho",
        ReceiptStatus.Cancelled => "Đã hủy",
        _ => s.ToString()
    };

    public static string ToDisplay(this InventoryType t) => t switch
    {
        InventoryType.Import => "Nhập kho",
        InventoryType.Export => "Xuất bán",
        InventoryType.Return => "Hoàn tồn",
        InventoryType.Adjust => "Điều chỉnh",
        _ => t.ToString()
    };

    public static string ToDisplay(this PostType t) => t switch
    {
        PostType.News => "Tin tức",
        PostType.Recruitment => "Tuyển dụng",
        _ => t.ToString()
    };
}
