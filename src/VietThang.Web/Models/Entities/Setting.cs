namespace VietThang.Web.Models.Entities;

/// <summary>Cấu hình hệ thống dạng key-value (tên cửa hàng, phí ship, ngưỡng freeship...).</summary>
public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
}

/// <summary>Các khóa cấu hình dùng trong hệ thống.</summary>
public static class SettingKeys
{
    public const string StoreName = "StoreName";
    public const string Hotline = "Hotline";
    public const string Email = "Email";
    public const string DefaultShippingFee = "DefaultShippingFee";
    public const string FreeShippingThreshold = "FreeShippingThreshold";
    public const string LowStockThreshold = "LowStockThreshold";
    public const string BankAccount = "BankAccount";
}
