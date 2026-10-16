using System.ComponentModel.DataAnnotations;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.ViewModels.Shop;

/// <summary>Thẻ sản phẩm trả về cho lưới danh mục / trang chủ.</summary>
public class ProductCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int DiscountPercent { get; set; }
    public bool HasDiscount => SalePrice < OriginalPrice;
    public bool IsNew { get; set; }
    public bool InStock { get; set; }
    public int SoldCount { get; set; }
}

/// <summary>Bộ lọc danh mục / tìm kiếm (KH03).</summary>
public class ProductFilter
{
    public string? Q { get; set; }
    public List<int> ColorIds { get; set; } = new();
    public List<int> SizeIds { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Material { get; set; }
    /// <summary>newest | price-asc | price-desc | bestseller</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
}

public class VariantOptionVm
{
    public int Id { get; set; }
    public int ColorId { get; set; }
    public int SizeId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int Stock { get; set; }
}

/// <summary>Thông tin giao hàng và thanh toán khi đặt hàng (KH07).</summary>
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Nhập họ tên người nhận"), StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Chọn tỉnh / thành phố"), StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chọn quận / huyện"), StringLength(100)]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chọn phường / xã"), StringLength(100)]
    public string Ward { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số nhà, tên đường"), StringLength(255)]
    public string Street { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;

    public bool SaveAddress { get; set; }
}
