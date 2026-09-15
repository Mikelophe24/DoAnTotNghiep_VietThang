using System.ComponentModel.DataAnnotations;

namespace VietThang.Web.ViewModels.Admin;

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên danh mục"), StringLength(100)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [StringLength(120)]
    [Display(Name = "Đường dẫn (slug)")]
    public string? Slug { get; set; }

    [Display(Name = "Danh mục cha")]
    public int? ParentId { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Đang hiển thị")]
    public bool IsActive { get; set; } = true;

    public string? ImageUrl { get; set; }

    [Display(Name = "Ảnh danh mục")]
    public IFormFile? ImageFile { get; set; }
}

public class ColorFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên màu"), StringLength(50)]
    [Display(Name = "Tên màu")]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mã màu dạng #RRGGBB")]
    [Display(Name = "Mã màu")]
    public string? HexCode { get; set; } = "#FFFFFF";
}

public class SizeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên size"), StringLength(20)]
    [Display(Name = "Tên size")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự")]
    public int DisplayOrder { get; set; }
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập mã sản phẩm"), StringLength(50)]
    [Display(Name = "Mã sản phẩm")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập tên sản phẩm"), StringLength(200)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [StringLength(220)]
    [Display(Name = "Đường dẫn (slug)")]
    public string? Slug { get; set; }

    [Required(ErrorMessage = "Chọn danh mục")]
    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [StringLength(100)]
    [Display(Name = "Chất liệu")]
    public string? Material { get; set; }

    [StringLength(500)]
    [Display(Name = "Mô tả ngắn")]
    public string? ShortDescription { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Giá không hợp lệ")]
    [Display(Name = "Giá bán (đ)")]
    public decimal Price { get; set; }

    [Display(Name = "Hàng mới về")]
    public bool IsNew { get; set; }

    [Display(Name = "Nổi bật")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Thêm ảnh")]
    public List<IFormFile>? ImageFiles { get; set; }
}

public class ProductListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int VariantCount { get; set; }
    public int TotalStock { get; set; }
    public bool IsActive { get; set; }
    public bool IsNew { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductListFilter
{
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public bool? Active { get; set; }
    public int Page { get; set; } = 1;
}

public class VariantEditViewModel
{
    public int Id { get; set; }

    [Range(0, 999_999_999, ErrorMessage = "Giá không hợp lệ")]
    public decimal? Price { get; set; }

    public bool IsActive { get; set; }
}
