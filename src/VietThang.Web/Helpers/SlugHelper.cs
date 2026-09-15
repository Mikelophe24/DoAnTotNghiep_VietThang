using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VietThang.Web.Helpers;

public static class SlugHelper
{
    /// <summary>"Bộ lanh nữ quần lửng" → "bo-lanh-nu-quan-lung"</summary>
    public static string ToSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = RemoveDiacritics(text).ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", "-");
        return s.Trim('-');
    }

    /// <summary>"Xanh mint" → "XANHMINT" (dùng ghép SKU).</summary>
    public static string ToCodePart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var s = RemoveDiacritics(text).ToUpperInvariant();
        return Regex.Replace(s, @"[^A-Z0-9]", "");
    }

    public static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch switch { 'đ' => 'd', 'Đ' => 'D', _ => ch });
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
