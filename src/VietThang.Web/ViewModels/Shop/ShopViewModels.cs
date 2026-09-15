using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Web.ViewModels.Shop;

/// <summary>Thẻ sản phẩm trong lưới danh mục / trang chủ.</summary>
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

public class HomeViewModel
{
    public List<Banner> Banners { get; set; } = new();
    public List<Category> RootCategories { get; set; } = new();
    public List<ProductCardVm> NewProducts { get; set; } = new();
    public List<ProductCardVm> SaleProducts { get; set; } = new();
    public List<ProductCardVm> BestSellers { get; set; } = new();
    public List<Post> Posts { get; set; } = new();
    public decimal FreeShippingThreshold { get; set; }
}

/// <summary>Bộ lọc trang danh mục / tìm kiếm (KH03).</summary>
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

    public bool HasAny => ColorIds.Count > 0 || SizeIds.Count > 0 || MinPrice.HasValue || MaxPrice.HasValue || !string.IsNullOrEmpty(Material);
}

public class ProductListViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Category? Category { get; set; }
    public List<Category> SubCategories { get; set; } = new();
    public List<(string Name, string Url)> Breadcrumb { get; set; } = new();
    public PagedList<ProductCardVm> Products { get; set; } = default!;
    public ProductFilter Filter { get; set; } = new();
    public List<Color> Colors { get; set; } = new();
    public List<Size> Sizes { get; set; } = new();
    public List<string> Materials { get; set; } = new();
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

public class ProductDetailViewModel
{
    public Product Product { get; set; } = default!;
    public PriceInfo Price { get; set; } = default!;
    public List<Color> Colors { get; set; } = new();
    public List<Size> Sizes { get; set; } = new();
    public List<VariantOptionVm> Variants { get; set; } = new();
    public string VariantsJson { get; set; } = "[]";
    public List<Review> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public List<ProductCardVm> Related { get; set; } = new();
    public int TotalStock => Variants.Sum(v => v.Stock);
}

/// <summary>Form thanh toán (KH07).</summary>
public class CheckoutViewModel
{
    [Required(ErrorMessage = "Nhập họ tên người nhận"), StringLength(100)]
    [Display(Name = "Họ và tên người nhận")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    [Display(Name = "Email (nhận xác nhận đơn)")]
    public string? Email { get; set; }

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

    [StringLength(500)]
    [Display(Name = "Ghi chú cho cửa hàng")]
    public string? Note { get; set; }

    [Display(Name = "Phương thức thanh toán")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cod;

    [Display(Name = "Lưu địa chỉ này cho lần sau")]
    public bool SaveAddress { get; set; }

    // Dữ liệu hiển thị, không bind từ form
    [ValidateNever] public CartSummary? Cart { get; set; }
    [ValidateNever] public List<Address> SavedAddresses { get; set; } = new();
    [ValidateNever] public string? BankAccount { get; set; }
}

public class OrderLookupViewModel
{
    [Required(ErrorMessage = "Nhập mã đơn hàng")]
    [Display(Name = "Mã đơn hàng")]
    public string OrderCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nhập số điện thoại đặt hàng")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [ValidateNever] public Order? Order { get; set; }
    public bool Searched { get; set; }
}

public class ContactViewModel
{
    [Required(ErrorMessage = "Nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [StringLength(200)]
    [Display(Name = "Chủ đề")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Nhập nội dung"), StringLength(2000)]
    [Display(Name = "Nội dung")]
    public string Message { get; set; } = string.Empty;
}
