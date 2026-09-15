namespace VietThang.Web.Models.Entities;

/// <summary>Biến thể màu × size của sản phẩm. Tồn kho quản lý ở cấp này.</summary>
public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int ColorId { get; set; }
    public Color Color { get; set; } = null!;
    public int SizeId { get; set; }
    public Size Size { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    /// <summary>Giá riêng của biến thể; null thì dùng Product.Price.</summary>
    public decimal? Price { get; set; }
    /// <summary>Giá vốn gần nhất, cập nhật khi duyệt phiếu nhập.</summary>
    public decimal CostPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
