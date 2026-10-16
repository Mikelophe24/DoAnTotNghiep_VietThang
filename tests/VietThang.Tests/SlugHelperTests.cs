using VietThang.Web.Helpers;

namespace VietThang.Tests;

/// <summary>Kiểm thử chuẩn hóa tiếng Việt: slug URL, mã SKU, chuỗi tìm kiếm không dấu.</summary>
public class SlugHelperTests
{
    [Theory]
    [InlineData("Bộ lanh nữ quần lửng", "bo-lanh-nu-quan-lung")]
    [InlineData("Đồ bộ đùi trẻ em", "do-bo-dui-tre-em")]
    [InlineData("  Áo thun   lạnh (cổ tim) 100% ", "ao-thun-lanh-co-tim-100")]
    [InlineData("TRUNG NIÊN", "trung-nien")]
    public void ToSlug_BoDauVaNoiBangGachNgang(string input, string expected)
        => Assert.Equal(expected, SlugHelper.ToSlug(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToSlug_ChuoiRong_TraVeRong(string? input)
        => Assert.Equal(string.Empty, SlugHelper.ToSlug(input));

    [Theory]
    [InlineData("Xanh mint", "XANHMINT")]
    [InlineData("Đỏ đô", "DODO")]
    [InlineData("Size 1 (1-2 tuổi)", "SIZE112TUOI")]
    [InlineData("3XL", "3XL")]
    public void ToCodePart_ChiGiuChuHoaVaSo(string input, string expected)
        => Assert.Equal(expected, SlugHelper.ToCodePart(input));

    [Fact]
    public void ToSearchText_GhepCacPhanKhongDauChuThuong()
        => Assert.Equal("bo lanh nu quan lung vt-bl-002 lanh", SlugHelper.ToSearchText("Bộ lanh nữ quần lửng", "VT-BL-002", null, "  Lanh "));

    [Theory]
    [InlineData("BỘ LANH", "bo lanh")]
    [InlineData("  áo   thun ", "ao thun")]
    [InlineData("khủng long", "khung long")]
    public void ToSearchText_TuKhoaNguoiDung_ChuanHoaGiongDuLieu(string input, string expected)
        => Assert.Equal(expected, SlugHelper.ToSearchText(input));

    [Fact]
    public void RemoveDiacritics_XuLyChuD()
        => Assert.Equal("Dong Da dong", SlugHelper.RemoveDiacritics("Đống Đa đồng"));
}
