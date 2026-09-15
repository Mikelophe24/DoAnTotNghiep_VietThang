using System.ComponentModel.DataAnnotations;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.ViewModels.Shop;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Giới tính")]
    public Gender? Gender { get; set; }

    [Display(Name = "Ngày sinh")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AddressFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên người nhận"), StringLength(100)]
    [Display(Name = "Người nhận")]
    public string ReceiverName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập tỉnh / thành phố"), StringLength(100)]
    [Display(Name = "Tỉnh / Thành phố")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập quận / huyện"), StringLength(100)]
    [Display(Name = "Quận / Huyện")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập phường / xã"), StringLength(100)]
    [Display(Name = "Phường / Xã")]
    public string Ward { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số nhà, tên đường"), StringLength(255)]
    [Display(Name = "Số nhà, tên đường")]
    public string Street { get; set; } = string.Empty;

    [Display(Name = "Đặt làm địa chỉ mặc định")]
    public bool IsDefault { get; set; }
}

public class ReviewFormViewModel
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    [Range(1, 5, ErrorMessage = "Chọn số sao từ 1 đến 5")] public byte Rating { get; set; } = 5;
    [StringLength(1000)] public string? Comment { get; set; }
}
