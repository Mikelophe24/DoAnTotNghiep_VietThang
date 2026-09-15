namespace VietThang.Web.Models.Entities;

/// <summary>Giỏ hàng lưu CSDL của khách đã đăng nhập (khách vãng lai dùng Session).</summary>
public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int Quantity { get; set; }
}
