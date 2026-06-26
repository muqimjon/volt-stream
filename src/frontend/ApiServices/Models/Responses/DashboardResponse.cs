namespace ApiServices.Models.Responses;

public record DashboardResponse
{
    public decimal TodaySalesAmount { get; set; }
    public int TodaySalesCount { get; set; }
    public decimal TodaySalesLength { get; set; }

    public decimal MonthSalesAmount { get; set; }
    public decimal MonthSalesLength { get; set; }

    public decimal TodayPaymentsAmount { get; set; }
    public int TodayPaymentsCount { get; set; }

    public List<TopCustomerResponse> TopCustomers { get; set; } = [];
    public List<TopProductResponse> TopSellingProducts { get; set; } = [];
    public List<DailySalesResponse> WeeklySales { get; set; } = [];
}

public record TopCustomerResponse
{
    public string Customer { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public record TopProductResponse
{
    public string Product { get; set; } = string.Empty;
    public decimal TotalLength { get; set; }
}

public record DailySalesResponse
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}
