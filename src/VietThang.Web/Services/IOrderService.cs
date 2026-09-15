using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.ViewModels.Shop;

namespace VietThang.Web.Services;

public record PlaceOrderResult(bool Success, string? OrderCode, string? Error);

/// <summary>Đặt hàng (UC-03): tạo đơn, trừ tồn, ghi nhật ký kho trong một transaction.</summary>
public interface IOrderService
{
    Task<PlaceOrderResult> PlaceOrderAsync(CheckoutViewModel model, string? userId);
    Task<string> GenerateOrderCodeAsync();
    /// <summary>Chuyển trạng thái đơn theo máy trạng thái; hủy thì hoàn tồn, hoàn thành thì đánh dấu đã thanh toán.</summary>
    Task<(bool Success, string Message)> ChangeStatusAsync(int orderId, OrderStatus to, string? note, string? actorUserId);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService _cart;
    private readonly IInventoryService _inventory;
    private readonly IAppEmailSender _email;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ApplicationDbContext db, ICartService cart, IInventoryService inventory, IAppEmailSender email, ILogger<OrderService> logger)
    {
        _db = db;
        _cart = cart;
        _inventory = inventory;
        _email = email;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> ChangeStatusAsync(int orderId, OrderStatus to, string? note, string? actorUserId)
    {
        var order = await _db.Orders.Include(o => o.Details).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return (false, "Không tìm thấy đơn hàng.");
        if (!OrderStateMachine.CanTransition(order.Status, to))
            return (false, $"Không thể chuyển từ \"{order.Status.ToDisplay()}\" sang \"{to.ToDisplay()}\".");

        await using var tx = await _db.Database.BeginTransactionAsync();
        var from = order.Status;
        if (to == OrderStatus.Cancelled)
        {
            await _inventory.ReturnStockForOrderAsync(order, actorUserId);
            order.CancelReason = note;
            if (order.PaymentStatus == PaymentStatus.Paid) order.PaymentStatus = PaymentStatus.Refunded;
        }
        if (to == OrderStatus.Completed)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.CompletedAt = DateTime.Now;
        }
        order.Status = to;
        order.StatusHistories.Add(new OrderStatusHistory { FromStatus = from, ToStatus = to, Note = note, ChangedByUserId = actorUserId });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        try
        {
            await _email.SendAsync(order.Email, $"[Việt Thắng] Đơn {order.OrderCode}: {to.ToDisplay()}",
                $"<p>Đơn hàng <b>{order.OrderCode}</b> của bạn đã chuyển sang trạng thái <b>{to.ToDisplay()}</b>.</p>{(string.IsNullOrEmpty(note) ? "" : $"<p>Ghi chú: {note}</p>")}");
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Không gửi được email trạng thái đơn {Code}", order.OrderCode); }

        return (true, $"Đơn {order.OrderCode} đã chuyển sang \"{to.ToDisplay()}\".");
    }

    public async Task<string> GenerateOrderCodeAsync()
    {
        var prefix = "VT" + DateTime.Now.ToString("yyMMdd");
        var last = await _db.Orders.Where(o => o.OrderCode.StartsWith(prefix))
            .OrderByDescending(o => o.OrderCode).Select(o => o.OrderCode).FirstOrDefaultAsync();
        var seq = last is null ? 1 : int.Parse(last.Substring(prefix.Length)) + 1;
        return prefix + seq.ToString("000");
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(CheckoutViewModel model, string? userId)
    {
        var summary = await _cart.GetSummaryAsync();
        if (summary.IsEmpty) return new PlaceOrderResult(false, null, "Giỏ hàng của bạn đang trống.");

        var unavailable = summary.Lines.FirstOrDefault(l => !l.IsAvailable);
        if (unavailable is not null)
            return new PlaceOrderResult(false, null,
                $"\"{unavailable.ProductName}\" ({unavailable.ColorName} / {unavailable.SizeName}) chỉ còn {unavailable.Stock} sản phẩm. Vui lòng cập nhật giỏ hàng.");

        if (!string.IsNullOrEmpty(summary.CouponCode) && summary.Coupon is null)
            return new PlaceOrderResult(false, null, summary.CouponError ?? "Mã giảm giá không hợp lệ.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                OrderCode = await GenerateOrderCodeAsync(),
                UserId = userId,
                CustomerName = model.CustomerName.Trim(),
                Phone = model.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                ShippingAddress = model.Street.Trim(),
                Province = model.Province.Trim(),
                District = model.District.Trim(),
                Ward = model.Ward.Trim(),
                Note = model.Note?.Trim(),
                SubTotal = summary.SubTotal,
                ShippingFee = summary.ShippingFee,
                DiscountAmount = summary.DiscountAmount,
                CouponId = summary.Coupon?.Id,
                TotalAmount = summary.Total,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = OrderStatus.Pending
            };

            var inventoryLogs = new List<InventoryTransaction>();
            foreach (var line in summary.Lines)
            {
                // Trừ tồn có điều kiện để chống bán âm khi đặt đồng thời
                var affected = await _db.ProductVariants
                    .Where(v => v.Id == line.VariantId && v.StockQuantity >= line.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.StockQuantity, v => v.StockQuantity - line.Quantity));
                if (affected == 0)
                    throw new InvalidOperationException($"\"{line.ProductName}\" ({line.ColorName} / {line.SizeName}) không còn đủ hàng.");

                var stockAfter = await _db.ProductVariants.Where(v => v.Id == line.VariantId).Select(v => v.StockQuantity).FirstAsync();
                await _db.Products.Where(p => p.Id == line.ProductId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.SoldCount, p => p.SoldCount + line.Quantity));

                order.Details.Add(new OrderDetail
                {
                    VariantId = line.VariantId,
                    ProductName = line.ProductName,
                    Sku = line.Sku,
                    ColorName = line.ColorName,
                    SizeName = line.SizeName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity
                });
                inventoryLogs.Add(new InventoryTransaction
                {
                    VariantId = line.VariantId,
                    Type = InventoryType.Export,
                    Quantity = -line.Quantity,
                    StockAfter = stockAfter,
                    ReferenceType = "Order",
                    Note = $"Xuất bán đơn {order.OrderCode}",
                    CreatedByUserId = userId
                });
            }

            if (summary.Coupon is not null)
            {
                var used = await _db.Coupons
                    .Where(c => c.Id == summary.Coupon.Id && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));
                if (used == 0) throw new InvalidOperationException("Mã giảm giá đã hết lượt sử dụng.");
            }

            order.StatusHistories.Add(new OrderStatusHistory
            {
                FromStatus = null,
                ToStatus = OrderStatus.Pending,
                Note = "Khách đặt hàng",
                ChangedByUserId = userId
            });

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var log in inventoryLogs) log.ReferenceId = order.Id;
            _db.InventoryTransactions.AddRange(inventoryLogs);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            await _cart.ClearAsync();
            _cart.SetCouponCode(null);

            try { await _email.SendAsync(order.Email, $"[Việt Thắng] Xác nhận đơn hàng {order.OrderCode}", BuildEmail(order)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Không gửi được email xác nhận đơn {Code}", order.OrderCode); }

            return new PlaceOrderResult(true, order.OrderCode, null);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            return new PlaceOrderResult(false, null, ex.Message);
        }
    }

    private static string BuildEmail(Order order)
    {
        var rows = string.Join("", order.Details.Select(d =>
            $"<tr><td>{d.ProductName} ({d.ColorName}/{d.SizeName})</td><td>{d.Quantity}</td><td>{d.UnitPrice.ToVnd()}</td></tr>"));
        return $"""
            <p>Chào {order.CustomerName}, cảm ơn bạn đã đặt hàng tại Thời trang Việt Thắng.</p>
            <p>Mã đơn: <b>{order.OrderCode}</b> · Thanh toán: {order.PaymentMethod.ToDisplay()}</p>
            <table border="1" cellpadding="6">{rows}</table>
            <p>Tiền hàng: {order.SubTotal.ToVnd()} · Phí ship: {order.ShippingFee.ToVnd()} · Giảm: {order.DiscountAmount.ToVnd()}</p>
            <p><b>Tổng thanh toán: {order.TotalAmount.ToVnd()}</b></p>
            <p>Giao tới: {order.ShippingAddress}, {order.Ward}, {order.District}, {order.Province}</p>
            """;
    }
}
