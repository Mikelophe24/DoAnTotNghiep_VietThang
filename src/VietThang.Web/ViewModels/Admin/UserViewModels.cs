using System.ComponentModel.DataAnnotations;

namespace VietThang.Web.ViewModels.Admin;

public class CustomerListItem
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class EmployeeFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email (dùng để đăng nhập)")]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Employee";

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu (để trống nếu không đổi)")]
    public string? Password { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class SettingsFormViewModel
{
    [Required(ErrorMessage = "Nhập tên cửa hàng"), StringLength(200)] [Display(Name = "Tên cửa hàng")] public string StoreName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nhập hotline"), StringLength(20)] [Display(Name = "Hotline")] public string Hotline { get; set; } = string.Empty;
    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)] [Display(Name = "Email liên hệ")] public string? Email { get; set; }
    [Range(0, 999_999_999)] [Display(Name = "Phí vận chuyển mặc định (đ)")] public decimal DefaultShippingFee { get; set; }
    [Range(0, 999_999_999)] [Display(Name = "Miễn phí vận chuyển cho đơn từ (đ)")] public decimal FreeShippingThreshold { get; set; }
    [Range(0, 100_000)] [Display(Name = "Ngưỡng cảnh báo sắp hết hàng")] public int LowStockThreshold { get; set; }
    [StringLength(500)] [Display(Name = "Thông tin chuyển khoản")] public string? BankAccount { get; set; }
}
