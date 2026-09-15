namespace VietThang.Web.ViewModels.Admin;

public record SeriesPoint(string Label, decimal Value, int Count = 0);
public record NamedValue(string Name, decimal Value, int Quantity = 0);

public class ReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    /// <summary>day | month</summary>
    public string GroupBy { get; set; } = "day";

    public decimal Revenue { get; set; }
    public int CompletedOrders { get; set; }
    public int TotalOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal AverageOrderValue => CompletedOrders == 0 ? 0 : Math.Round(Revenue / CompletedOrders, 0);
    public int ItemsSold { get; set; }
    public int NewCustomers { get; set; }

    public List<SeriesPoint> RevenueSeries { get; set; } = new();
    public List<NamedValue> StatusDistribution { get; set; } = new();
    public List<NamedValue> TopProducts { get; set; } = new();
    public List<NamedValue> CategoryRevenue { get; set; } = new();
    public List<SeriesPoint> ProfitSeries { get; set; } = new();
    public List<SeriesPoint> NewCustomerSeries { get; set; } = new();

    public int StockUnits { get; set; }
    public decimal StockValue { get; set; }
    public int LowStockCount { get; set; }
}
