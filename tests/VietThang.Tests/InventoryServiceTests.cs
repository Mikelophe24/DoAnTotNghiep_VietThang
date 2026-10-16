using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;

namespace VietThang.Tests;

/// <summary>Kiểm thử nghiệp vụ kho: điều chỉnh kiểm kê và duyệt phiếu nhập (UC-13).</summary>
public class InventoryServiceTests
{
    private static async Task<(ApplicationDbContext Db, ProductVariant Variant)> SeedVariantAsync(int stock = 10, decimal cost = 100000)
    {
        var db = TestDb.Create();
        var variant = new ProductVariant
        {
            Sku = "VT-TEST-XANH-M", StockQuantity = stock, CostPrice = cost,
            Product = new Product { Code = "VT-TEST", Name = "Sản phẩm thử", Slug = "san-pham-thu", Price = 200000 },
            Color = new Color { Name = "Xanh" }, Size = new Size { Name = "M" }
        };
        db.ProductVariants.Add(variant);
        await db.SaveChangesAsync();
        return (db, variant);
    }

    [Fact]
    public async Task AdjustStock_TangTon_GhiNhatKyDuong()
    {
        var (db, variant) = await SeedVariantAsync(stock: 10);

        await new InventoryService(db).AdjustStockAsync(variant.Id, 13, "Kiểm kê thừa 3", "user-1");

        Assert.Equal(13, variant.StockQuantity);
        var log = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(InventoryType.Adjust, log.Type);
        Assert.Equal(3, log.Quantity);
        Assert.Equal(13, log.StockAfter);
        Assert.Equal("Kiểm kê thừa 3", log.Note);
    }

    [Fact]
    public async Task AdjustStock_GiamTon_GhiNhatKyAm()
    {
        var (db, variant) = await SeedVariantAsync(stock: 10);

        await new InventoryService(db).AdjustStockAsync(variant.Id, 8, null, "user-1");

        var log = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(-2, log.Quantity);
        Assert.Equal(8, log.StockAfter);
        Assert.False(string.IsNullOrEmpty(log.Note));   // có ghi chú mặc định
    }

    [Fact]
    public async Task AdjustStock_KhongDoi_KhongGhiNhatKy()
    {
        var (db, variant) = await SeedVariantAsync(stock: 10);

        await new InventoryService(db).AdjustStockAsync(variant.Id, 10, null, "user-1");

        Assert.Empty(db.InventoryTransactions);
    }

    [Fact]
    public async Task AdjustStock_SoAm_BaoLoi()
    {
        var (db, variant) = await SeedVariantAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new InventoryService(db).AdjustStockAsync(variant.Id, -1, null, "user-1"));
        Assert.Equal(10, variant.StockQuantity);
    }

    [Fact]
    public async Task CompleteReceipt_CongTon_CapNhatGiaVon_GhiNhatKy()
    {
        var (db, variant) = await SeedVariantAsync(stock: 3, cost: 100000);
        var receipt = new GoodsReceipt { Code = "PN001", CreatedByUserId = "user-1", Supplier = new Supplier { Name = "Xưởng may" }, Status = ReceiptStatus.Draft };
        receipt.Details.Add(new GoodsReceiptDetail { VariantId = variant.Id, Quantity = 10, UnitCost = 150000 });
        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync();

        await new InventoryService(db).CompleteReceiptAsync(receipt.Id, "user-1");

        Assert.Equal(13, variant.StockQuantity);
        Assert.Equal(150000, variant.CostPrice);
        Assert.Equal(ReceiptStatus.Completed, receipt.Status);
        Assert.Equal(1500000, receipt.TotalAmount);
        Assert.NotNull(receipt.CompletedAt);
        var log = await db.InventoryTransactions.SingleAsync();
        Assert.Equal(InventoryType.Import, log.Type);
        Assert.Equal(10, log.Quantity);
        Assert.Equal(13, log.StockAfter);
        Assert.Equal(receipt.Id, log.ReferenceId);
    }

    [Fact]
    public async Task CompleteReceipt_PhieuDaDuyet_KhongDuyetLai()
    {
        var (db, variant) = await SeedVariantAsync(stock: 3);
        var receipt = new GoodsReceipt { Code = "PN002", CreatedByUserId = "user-1", Supplier = new Supplier { Name = "Xưởng may" }, Status = ReceiptStatus.Completed };
        receipt.Details.Add(new GoodsReceiptDetail { VariantId = variant.Id, Quantity = 10, UnitCost = 150000 });
        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new InventoryService(db).CompleteReceiptAsync(receipt.Id, "user-1"));
        Assert.Equal(3, variant.StockQuantity);   // tồn không bị cộng hai lần
    }

    [Fact]
    public async Task CompleteReceipt_PhieuKhongCoDong_BaoLoi()
    {
        var (db, _) = await SeedVariantAsync();
        var receipt = new GoodsReceipt { Code = "PN003", CreatedByUserId = "user-1", Supplier = new Supplier { Name = "Xưởng may" }, Status = ReceiptStatus.Draft };
        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new InventoryService(db).CompleteReceiptAsync(receipt.Id, "user-1"));
    }
}
