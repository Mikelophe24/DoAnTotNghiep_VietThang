using System.ComponentModel.DataAnnotations;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.ViewModels.Admin;

public class PromotionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên chương trình"), StringLength(150)]
    [Display(Name = "Tên chương trình")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Loại giảm")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;

    [Range(1, 999_999_999, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; } = 10;

    [Display(Name = "Bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Đang kích hoạt")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sản phẩm áp dụng")]
    public List<int> ProductIds { get; set; } = new();
}

public class CouponFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập mã"), StringLength(30)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "Mã chỉ gồm chữ và số")]
    [Display(Name = "Mã giảm giá")]
    public string Code { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Loại giảm")]
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;

    [Range(1, 999_999_999, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; } = 10;

    [Range(0, 999_999_999)]
    [Display(Name = "Giảm tối đa (đ, chỉ với %)")]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(0, 999_999_999)]
    [Display(Name = "Đơn tối thiểu (đ)")]
    public decimal MinOrderAmount { get; set; }

    [Range(1, 1_000_000)]
    [Display(Name = "Giới hạn lượt dùng (trống = không giới hạn)")]
    public int? UsageLimit { get; set; }

    [Display(Name = "Bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Kết thúc")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

    [Display(Name = "Đang kích hoạt")]
    public bool IsActive { get; set; } = true;
}

public class PostFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề"), StringLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [StringLength(220)]
    [Display(Name = "Đường dẫn (slug)")]
    public string? Slug { get; set; }

    [StringLength(500)]
    [Display(Name = "Tóm tắt")]
    public string? Summary { get; set; }

    [Required(ErrorMessage = "Nhập nội dung")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Loại bài")]
    public PostType Type { get; set; } = PostType.News;

    [Display(Name = "Xuất bản")]
    public bool IsPublished { get; set; } = true;

    public string? ThumbnailUrl { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? ThumbnailFile { get; set; }
}

public class BannerFormViewModel
{
    public int Id { get; set; }

    [StringLength(150)]
    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [StringLength(500)]
    [Display(Name = "Liên kết khi bấm")]
    public string? LinkUrl { get; set; }

    [Display(Name = "Vị trí")]
    public BannerPosition Position { get; set; } = BannerPosition.HomeSlider;

    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Hiển thị")]
    public bool IsActive { get; set; } = true;

    public string? ImageUrl { get; set; }

    [Display(Name = "Ảnh banner (1600×600)")]
    public IFormFile? ImageFile { get; set; }
}

public class StoreFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên cửa hàng"), StringLength(150)]
    [Display(Name = "Tên cửa hàng")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập địa chỉ"), StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)] [Display(Name = "Điện thoại")] public string? Phone { get; set; }
    [StringLength(100)] [Display(Name = "Giờ mở cửa")] public string? OpeningHours { get; set; }
    [StringLength(1000)] [Display(Name = "Link nhúng Google Maps")] public string? MapEmbedUrl { get; set; }
    [Display(Name = "Đang hoạt động")] public bool IsActive { get; set; } = true;
}
