namespace VietThang.Web.Models.Enums;

/// <summary>Trạng thái đơn hàng. Chuyển trạng thái hợp lệ xem OrderStateMachine.</summary>
public enum OrderStatus : byte
{
    Pending = 0,    // Chờ xác nhận
    Confirmed = 1,  // Đã xác nhận
    Shipping = 2,   // Đang giao
    Completed = 3,  // Hoàn thành
    Cancelled = 4   // Đã hủy
}

public enum PaymentMethod : byte
{
    Cod = 1,           // Thanh toán khi nhận hàng
    BankTransfer = 2   // Chuyển khoản ngân hàng
}

public enum PaymentStatus : byte
{
    Unpaid = 0,
    Paid = 1,
    Refunded = 2
}

public enum DiscountType : byte
{
    Percent = 1,  // Giảm theo %
    Amount = 2    // Giảm số tiền cố định
}

public enum ReceiptStatus : byte
{
    Draft = 0,      // Nháp
    Completed = 1,  // Đã nhập kho
    Cancelled = 2   // Đã hủy
}

public enum InventoryType : byte
{
    Import = 1,  // Nhập kho từ phiếu nhập
    Export = 2,  // Xuất bán theo đơn
    Return = 3,  // Hoàn tồn do hủy đơn
    Adjust = 4   // Điều chỉnh kiểm kê
}

public enum PostType : byte
{
    News = 1,         // Tin tức
    Recruitment = 2   // Tuyển dụng
}

public enum BannerPosition : byte
{
    HomeSlider = 1,
    HomeMiddle = 2,
    CategoryTop = 3
}

public enum Gender : byte
{
    Other = 0,
    Male = 1,
    Female = 2
}
