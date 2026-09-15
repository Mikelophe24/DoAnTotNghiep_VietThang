namespace VietThang.Web.Models.Entities;

/// <summary>Đánh giá sản phẩm của khách đã mua, cần duyệt trước khi hiển thị.</summary>
public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    /// <summary>1..5 sao</summary>
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
