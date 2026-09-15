namespace VietThang.Web.Models.Entities;

/// <summary>Liên hệ gửi từ form trên website.</summary>
public class Contact
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsHandled { get; set; }
    public string? HandledByUserId { get; set; }
    public ApplicationUser? HandledBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
