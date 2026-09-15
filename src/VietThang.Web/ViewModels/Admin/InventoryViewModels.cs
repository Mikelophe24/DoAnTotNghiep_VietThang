using System.ComponentModel.DataAnnotations;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.ViewModels.Admin;

public class SupplierFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên nhà cung cấp"), StringLength(150)]
    [Display(Name = "Tên nhà cung cấp")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)] [Display(Name = "Người liên hệ")] public string? ContactName { get; set; }
    [StringLength(20)] [Display(Name = "Điện thoại")] public string? Phone { get; set; }
    [StringLength(100), EmailAddress(ErrorMessage = "Email không hợp lệ")] [Display(Name = "Email")] public string? Email { get; set; }
    [StringLength(255)] [Display(Name = "Địa chỉ")] public string? Address { get; set; }
    [Display(Name = "Đang hợp tác")] public bool IsActive { get; set; } = true;
}

public class GoodsReceiptFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Chọn nhà cung cấp")]
    [Display(Name = "Nhà cung cấp")]
    public int? SupplierId { get; set; }

    [Display(Name = "Ngày nhập")]
    [DataType(DataType.Date)]
    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public class ReceiptLineViewModel
{
    public int ReceiptId { get; set; }
    [Required(ErrorMessage = "Chọn biến thể")] public int? VariantId { get; set; }
    [Range(1, 100000, ErrorMessage = "Số lượng phải lớn hơn 0")] public int Quantity { get; set; } = 1;
    [Range(0, 999_999_999, ErrorMessage = "Giá vốn không hợp lệ")] public decimal UnitCost { get; set; }
}

public class ReceiptListFilter
{
    public ReceiptStatus? Status { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}

public class InventoryFilter
{
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public bool LowOnly { get; set; }
    public int Page { get; set; } = 1;
}

public class InventoryRow
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class StockAdjustViewModel
{
    public int VariantId { get; set; }
    [Range(0, 1_000_000, ErrorMessage = "Số lượng không hợp lệ")] public int NewQuantity { get; set; }
    [StringLength(255)] public string? Note { get; set; }
}

public class HistoryFilter
{
    public int? VariantId { get; set; }
    public InventoryType? Type { get; set; }
    public string? Q { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}

public class OrderListFilter
{
    public OrderStatus? Status { get; set; }
    public string? Q { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
}

public class ChangeStatusViewModel
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    [StringLength(255)] public string? Note { get; set; }
}
