using System.ComponentModel.DataAnnotations;
using VietThang.Web.Models.Enums;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.ViewModels.Api;

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)] public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")] public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập số điện thoại"), RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")] public string PhoneNumber { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập mật khẩu"), StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")] public string Password { get; set; } = string.Empty;
}

public record AuthResponse(string Token, DateTime ExpiresAt, UserInfo User);
public record UserInfo(string Id, string FullName, string? Email, string? Phone, IList<string> Roles);

public class CartItemRequest
{
    [Range(1, int.MaxValue)] public int VariantId { get; set; }
    [Range(1, 999)] public int Quantity { get; set; } = 1;
}

/// <summary>Tính giá giỏ hàng cho khách vãng lai (giỏ lưu ở trình duyệt) hoặc khách đăng nhập kèm mã giảm giá.</summary>
public class QuoteRequest
{
    public List<CartItemRequest> Items { get; set; } = new();
    public string? CouponCode { get; set; }
}

public class CheckoutRequest : CheckoutViewModel
{
    /// <summary>Khách vãng lai gửi giỏ từ trình duyệt; khách đăng nhập để trống để dùng giỏ trong CSDL.</summary>
    public List<CartItemRequest>? Items { get; set; }
    public string? CouponCode { get; set; }
}

public class CancelOrderRequest
{
    [StringLength(200)] public string? Reason { get; set; }
}

public class ReviewRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 5)] public byte Rating { get; set; } = 5;
    [StringLength(1000)] public string? Comment { get; set; }
}

public class ContactRequest
{
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)] public string FullName { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)] public string? Email { get; set; }
    [StringLength(20)] public string? Phone { get; set; }
    [StringLength(200)] public string? Subject { get; set; }
    [Required(ErrorMessage = "Nhập nội dung"), StringLength(2000)] public string Message { get; set; } = string.Empty;
}
