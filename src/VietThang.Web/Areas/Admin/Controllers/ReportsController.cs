using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VietThang.Web.Data;
using VietThang.Web.Helpers;
using VietThang.Web.Models.Entities;
using VietThang.Web.Models.Enums;
using VietThang.Web.Services;
using VietThang.Web.ViewModels.Admin;

namespace VietThang.Web.Areas.Admin.Controllers;

/// <summary>QT20 – Thống kê báo cáo (chỉ Admin). Doanh thu tính theo đơn Hoàn thành, mốc CompletedAt.</summary>
[Authorize(Roles = SeedData.RoleAdmin)]
public class ReportsController : AdminBaseController
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingService _settings;

    public ReportsController(ApplicationDbContext db, ISettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to, string groupBy = "day")
    {
        var f = (from ?? DateTime.Today.AddDays(-29)).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) (f, t) = (t, f);
        var tEnd = t.AddDays(1);
        if (groupBy != "month") groupBy = "day";

        var vm = new ReportViewModel { From = f, To = t, GroupBy = groupBy };

        var completed = _db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Completed && o.CompletedAt >= f && o.CompletedAt < tEnd);
        var details = _db.OrderDetails.AsNoTracking().Where(d => d.Order.Status == OrderStatus.Completed && d.Order.CompletedAt >= f && d.Order.CompletedAt < tEnd);

        vm.Revenue = await completed.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        vm.CompletedOrders = await completed.CountAsync();
        vm.TotalOrders = await _db.Orders.CountAsync(o => o.CreatedAt >= f && o.CreatedAt < tEnd);
        vm.CancelledOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Cancelled && o.CreatedAt >= f && o.CreatedAt < tEnd);
        vm.ItemsSold = await details.SumAsync(d => (int?)d.Quantity) ?? 0;
        vm.GrossProfit = await details.SumAsync(d => (decimal?)((d.UnitPrice - d.Variant.CostPrice) * d.Quantity)) ?? 0;

        // Doanh thu theo ngày / tháng, điền đủ các mốc không có đơn
        if (groupBy == "month")
        {
            var rows = await completed.GroupBy(o => new { o.CompletedAt!.Value.Year, o.CompletedAt.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sum = g.Sum(o => o.TotalAmount), Count = g.Count() }).ToListAsync();
            for (var m = new DateTime(f.Year, f.Month, 1); m <= t; m = m.AddMonths(1))
            {
                var r = rows.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
                vm.RevenueSeries.Add(new SeriesPoint(m.ToString("MM/yyyy"), r?.Sum ?? 0, r?.Count ?? 0));
            }
        }
        else
        {
            var rows = await completed.GroupBy(o => o.CompletedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Sum = g.Sum(o => o.TotalAmount), Count = g.Count() }).ToListAsync();
            for (var d = f; d <= t; d = d.AddDays(1))
            {
                var r = rows.FirstOrDefault(x => x.Date == d);
                vm.RevenueSeries.Add(new SeriesPoint(d.ToString("dd/MM"), r?.Sum ?? 0, r?.Count ?? 0));
            }
        }

        vm.StatusDistribution = (await _db.Orders.AsNoTracking().Where(o => o.CreatedAt >= f && o.CreatedAt < tEnd)
                .GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync())
            .OrderBy(x => x.Key).Select(x => new NamedValue(x.Key.ToDisplay(), x.Count, x.Count)).ToList();

        vm.TopProducts = (await details.GroupBy(d => d.Variant.Product.Name)
                .Select(g => new { Name = g.Key, Qty = g.Sum(d => d.Quantity), Rev = g.Sum(d => d.UnitPrice * d.Quantity) })
                .OrderByDescending(x => x.Qty).Take(10).ToListAsync())
            .Select(x => new NamedValue(x.Name, x.Rev, x.Qty)).ToList();

        vm.CategoryRevenue = (await details
                .GroupBy(d => d.Variant.Product.Category.Parent != null ? d.Variant.Product.Category.Parent.Name : d.Variant.Product.Category.Name)
                .Select(g => new { Name = g.Key, Rev = g.Sum(d => d.UnitPrice * d.Quantity), Qty = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.Rev).ToListAsync())
            .Select(x => new NamedValue(x.Name, x.Rev, x.Qty)).ToList();

        var profitRows = await details.GroupBy(d => new { d.Order.CompletedAt!.Value.Year, d.Order.CompletedAt.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Profit = g.Sum(d => (d.UnitPrice - d.Variant.CostPrice) * d.Quantity), Rev = g.Sum(d => d.UnitPrice * d.Quantity) }).ToListAsync();
        for (var m = new DateTime(f.Year, f.Month, 1); m <= t; m = m.AddMonths(1))
        {
            var r = profitRows.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            vm.ProfitSeries.Add(new SeriesPoint(m.ToString("MM/yyyy"), r?.Profit ?? 0, (int)(r?.Rev ?? 0)));
        }

        var staffIds = await (from ur in _db.UserRoles join r in _db.Roles on ur.RoleId equals r.Id
                              where r.Name == SeedData.RoleAdmin || r.Name == SeedData.RoleEmployee select ur.UserId).ToListAsync();
        var customerRows = await _db.Users.AsNoTracking().Where(u => !staffIds.Contains(u.Id) && u.CreatedAt >= f && u.CreatedAt < tEnd)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month }).Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() }).ToListAsync();
        vm.NewCustomers = customerRows.Sum(x => x.Count);
        for (var m = new DateTime(f.Year, f.Month, 1); m <= t; m = m.AddMonths(1))
        {
            var r = customerRows.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);
            vm.NewCustomerSeries.Add(new SeriesPoint(m.ToString("MM/yyyy"), r?.Count ?? 0, r?.Count ?? 0));
        }

        var low = await _settings.GetIntAsync(SettingKeys.LowStockThreshold, 5);
        vm.StockUnits = await _db.ProductVariants.SumAsync(v => v.StockQuantity);
        vm.StockValue = await _db.ProductVariants.SumAsync(v => v.StockQuantity * v.CostPrice);
        vm.LowStockCount = await _db.ProductVariants.CountAsync(v => v.IsActive && v.StockQuantity <= low);

        ViewBag.ChartJson = JsonSerializer.Serialize(new
        {
            revenue = vm.RevenueSeries.Select(p => new { p.Label, p.Value, p.Count }),
            status = vm.StatusDistribution.Select(s => new { s.Name, s.Value }),
            top = vm.TopProducts.Select(p => new { p.Name, p.Value, p.Quantity }),
            category = vm.CategoryRevenue.Select(c => new { c.Name, c.Value }),
            profit = vm.ProfitSeries.Select(p => new { p.Label, p.Value, revenue = p.Count }),
            customers = vm.NewCustomerSeries.Select(p => new { p.Label, p.Value })
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return View(vm);
    }

    /// <summary>Xuất Excel: đơn hàng trong kỳ và doanh thu theo ngày.</summary>
    public async Task<IActionResult> Export(DateTime? from, DateTime? to)
    {
        var f = (from ?? DateTime.Today.AddDays(-29)).Date;
        var t = (to ?? DateTime.Today).Date;
        var tEnd = t.AddDays(1);

        var orders = await _db.Orders.AsNoTracking().Include(o => o.Details).Include(o => o.Coupon)
            .Where(o => o.CreatedAt >= f && o.CreatedAt < tEnd).OrderBy(o => o.CreatedAt).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Đơn hàng");
        var headers = new[] { "Mã đơn", "Ngày đặt", "Khách hàng", "Điện thoại", "Địa chỉ", "Số SP", "Tiền hàng", "Phí ship", "Giảm giá", "Tổng thanh toán", "Thanh toán", "Trạng thái", "Hoàn thành lúc" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(1, i + 1).Value = headers[i];
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
        int row = 2;
        foreach (var o in orders)
        {
            ws.Cell(row, 1).Value = o.OrderCode;
            ws.Cell(row, 2).Value = o.CreatedAt;
            ws.Cell(row, 3).Value = o.CustomerName;
            ws.Cell(row, 4).Value = o.Phone;
            ws.Cell(row, 5).Value = $"{o.ShippingAddress}, {o.Ward}, {o.District}, {o.Province}";
            ws.Cell(row, 6).Value = o.Details.Sum(d => d.Quantity);
            ws.Cell(row, 7).Value = o.SubTotal;
            ws.Cell(row, 8).Value = o.ShippingFee;
            ws.Cell(row, 9).Value = o.DiscountAmount;
            ws.Cell(row, 10).Value = o.TotalAmount;
            ws.Cell(row, 11).Value = o.PaymentMethod.ToDisplay() + " - " + o.PaymentStatus.ToDisplay();
            ws.Cell(row, 12).Value = o.Status.ToDisplay();
            ws.Cell(row, 13).Value = o.CompletedAt;
            row++;
        }
        ws.Range(2, 2, Math.Max(row - 1, 2), 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        ws.Range(2, 13, Math.Max(row - 1, 2), 13).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        ws.Range(2, 7, Math.Max(row - 1, 2), 10).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();

        var ws2 = wb.Worksheets.Add("Chi tiết sản phẩm");
        var h2 = new[] { "Mã đơn", "Ngày", "Trạng thái đơn", "Sản phẩm", "SKU", "Màu", "Size", "Số lượng", "Đơn giá", "Thành tiền" };
        for (int i = 0; i < h2.Length; i++) ws2.Cell(1, i + 1).Value = h2[i];
        ws2.Row(1).Style.Font.Bold = true;
        ws2.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;
        row = 2;
        foreach (var o in orders)
        foreach (var d in o.Details)
        {
            ws2.Cell(row, 1).Value = o.OrderCode;
            ws2.Cell(row, 2).Value = o.CreatedAt;
            ws2.Cell(row, 3).Value = o.Status.ToDisplay();
            ws2.Cell(row, 4).Value = d.ProductName;
            ws2.Cell(row, 5).Value = d.Sku;
            ws2.Cell(row, 6).Value = d.ColorName;
            ws2.Cell(row, 7).Value = d.SizeName;
            ws2.Cell(row, 8).Value = d.Quantity;
            ws2.Cell(row, 9).Value = d.UnitPrice;
            ws2.Cell(row, 10).Value = d.LineTotal;
            row++;
        }
        ws2.Range(2, 2, Math.Max(row - 1, 2), 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
        ws2.Range(2, 9, Math.Max(row - 1, 2), 10).Style.NumberFormat.Format = "#,##0";
        ws2.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"bao-cao-don-hang-{f:yyyyMMdd}-{t:yyyyMMdd}.xlsx");
    }
}
