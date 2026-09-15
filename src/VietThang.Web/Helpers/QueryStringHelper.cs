using Microsoft.AspNetCore.WebUtilities;

namespace VietThang.Web.Helpers;

public static class QueryStringHelper
{
    /// <summary>Giữ nguyên query string hiện tại, chỉ thay tham số page (dùng cho phân trang có bộ lọc).</summary>
    public static string WithPage(this HttpRequest request, int page)
    {
        var pairs = QueryHelpers.ParseQuery(request.QueryString.Value ?? string.Empty)
            .Where(kv => !string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase))
            .SelectMany(kv => kv.Value.Select(v => (kv.Key, Value: v ?? string.Empty)))
            .ToList();
        pairs.Add(("page", page.ToString()));
        return request.Path + "?" + string.Join("&", pairs.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value)));
    }

    /// <summary>Định dạng tiền VNĐ: 275000 → "275.000 đ".</summary>
    public static string ToVnd(this decimal amount) => amount.ToString("N0") + " đ";
}
