namespace VietThang.Web.Models.Entities;

/// <summary>Sản phẩm yêu thích (khóa chính kép UserId + ProductId).</summary>
public class Wishlist
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
