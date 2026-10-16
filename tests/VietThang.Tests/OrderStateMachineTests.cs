using VietThang.Web.Helpers;
using VietThang.Web.Models.Enums;

namespace VietThang.Tests;

/// <summary>Kiểm thử máy trạng thái đơn hàng (mục 4.10 tài liệu kiến trúc).</summary>
public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipping)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Shipping, OrderStatus.Completed)]
    public void CanTransition_ChuyenHopLe_TraVeTrue(OrderStatus from, OrderStatus to)
        => Assert.True(OrderStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipping)]      // không được bỏ qua bước xác nhận
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]     // không quay lui
    [InlineData(OrderStatus.Shipping, OrderStatus.Cancelled)]    // đang giao không hủy được
    [InlineData(OrderStatus.Shipping, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled)]   // trạng thái cuối
    [InlineData(OrderStatus.Completed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending)]       // không chuyển sang chính nó
    public void CanTransition_ChuyenKhongHopLe_TraVeFalse(OrderStatus from, OrderStatus to)
        => Assert.False(OrderStateMachine.CanTransition(from, to));

    [Theory]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.Shipping, false)]
    public void IsFinal_ChiHoanThanhVaDaHuyLaTrangThaiCuoi(OrderStatus status, bool expected)
        => Assert.Equal(expected, OrderStateMachine.IsFinal(status));

    [Fact]
    public void NextStatuses_ChoXacNhan_CoXacNhanVaHuy()
        => Assert.Equal(new[] { OrderStatus.Confirmed, OrderStatus.Cancelled }, OrderStateMachine.NextStatuses(OrderStatus.Pending));

    [Fact]
    public void NextStatuses_DangGiao_ChiCoHoanThanh()
        => Assert.Equal(new[] { OrderStatus.Completed }, OrderStateMachine.NextStatuses(OrderStatus.Shipping));

    [Fact]
    public void MoiTrangThaiDeuDuocKhaiBaoTrongMayTrangThai()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
            Assert.NotNull(OrderStateMachine.NextStatuses(status));
    }

    [Theory]
    [InlineData(OrderStatus.Pending, "Chờ xác nhận")]
    [InlineData(OrderStatus.Confirmed, "Đã xác nhận")]
    [InlineData(OrderStatus.Shipping, "Đang giao hàng")]
    [InlineData(OrderStatus.Completed, "Hoàn thành")]
    [InlineData(OrderStatus.Cancelled, "Đã hủy")]
    public void ToDisplay_TenTiengViet(OrderStatus status, string expected)
        => Assert.Equal(expected, status.ToDisplay());
}
