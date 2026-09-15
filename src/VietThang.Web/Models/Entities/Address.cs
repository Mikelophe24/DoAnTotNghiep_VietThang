namespace VietThang.Web.Models.Entities;

/// <summary>Sổ địa chỉ giao hàng của khách hàng.</summary>
public class Address
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string ReceiverName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public string FullAddress => $"{Street}, {Ward}, {District}, {Province}";
}
