using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;

namespace VietThang.Web.Services;

/// <summary>Nghiệp vụ kho (QT13, QT14): duyệt phiếu nhập, điều chỉnh tồn, hoàn tồn khi hủy đơn. Mọi biến động đều ghi InventoryTransactions.</summary>
public interface IInventoryService
{
    Task<string> GenerateReceiptCodeAsync();
    Task CompleteReceiptAsync(int receiptId, string userId);
    Task AdjustStockAsync(int variantId, int newQuantity, string? note, string userId);
    /// <summary>Hoàn tồn cho đơn bị hủy. Gọi bên trong transaction của người gọi.</summary>
    Task ReturnStockForOrderAsync(Order order, string? userId);
}

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _db;
    public InventoryService(ApplicationDbContext db) => _db = db;

    public async Task<string> GenerateReceiptCodeAsync()
    {
        var prefix = "PN" + DateTime.Now.ToString("yyMMdd");
        var last = await _db.GoodsReceipts.Where(r => r.Code.StartsWith(prefix))
            .OrderByDescending(r => r.Code).Select(r => r.Code).FirstOrDefaultAsync();
        var seq = last is null ? 1 : int.Parse(last.Substring(prefix.Length)) + 1;
        return prefix + seq.ToString("000");
    }

    public async Task CompleteReceiptAsync(int receiptId, string userId)
    {
        var receipt = await _db.GoodsReceipts.Include(r => r.Details).ThenInclude(d => d.Variant)
            .FirstOrDefaultAsync(r => r.Id == receiptId)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập.");
        if (receipt.Status != ReceiptStatus.Draft) throw new InvalidOperationException("Chỉ duyệt được phiếu ở trạng thái Nháp.");
        if (receipt.Details.Count == 0) throw new InvalidOperationException("Phiếu nhập chưa có dòng hàng nào.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        foreach (var d in receipt.Details)
        {
            d.Variant.StockQuantity += d.Quantity;
            d.Variant.CostPrice = d.UnitCost;
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                VariantId = d.VariantId,
                Type = InventoryType.Import,
                Quantity = d.Quantity,
                StockAfter = d.Variant.StockQuantity,
                ReferenceType = "GoodsReceipt",
                ReferenceId = receipt.Id,
                Note = $"Nhập kho phiếu {receipt.Code}",
                CreatedByUserId = userId
            });
        }
        receipt.TotalAmount = receipt.Details.Sum(d => d.Quantity * d.UnitCost);
        receipt.Status = ReceiptStatus.Completed;
        receipt.CompletedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task AdjustStockAsync(int variantId, int newQuantity, string? note, string userId)
    {
        if (newQuantity < 0) throw new InvalidOperationException("Tồn kho không được âm.");
        var variant = await _db.ProductVariants.FindAsync(variantId)
            ?? throw new InvalidOperationException("Không tìm thấy biến thể.");
        var diff = newQuantity - variant.StockQuantity;
        if (diff == 0) return;
        variant.StockQuantity = newQuantity;
        _db.InventoryTransactions.Add(new InventoryTransaction
        {
            VariantId = variantId,
            Type = InventoryType.Adjust,
            Quantity = diff,
            StockAfter = newQuantity,
            ReferenceType = "Adjustment",
            Note = string.IsNullOrWhiteSpace(note) ? "Điều chỉnh kiểm kê" : note.Trim(),
            CreatedByUserId = userId
        });
        await _db.SaveChangesAsync();
    }

    public async Task ReturnStockForOrderAsync(Order order, string? userId)
    {
        foreach (var d in order.Details)
        {
            var variant = await _db.ProductVariants.FindAsync(d.VariantId);
            if (variant is null) continue;
            variant.StockQuantity += d.Quantity;
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                VariantId = d.VariantId,
                Type = InventoryType.Return,
                Quantity = d.Quantity,
                StockAfter = variant.StockQuantity,
                ReferenceType = "Order",
                ReferenceId = order.Id,
                Note = $"Hoàn tồn do hủy đơn {order.OrderCode}",
                CreatedByUserId = userId
            });
            await _db.Products.Where(p => p.Id == variant.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.SoldCount, p => p.SoldCount - d.Quantity));
        }
        if (order.CouponId.HasValue)
        {
            await _db.Coupons.Where(c => c.Id == order.CouponId && c.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
        }
    }
}
