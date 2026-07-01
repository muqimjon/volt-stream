namespace VoltStream.Application.Features.Dashboard.DTOs;

/// <summary>
/// Bosh sahifa (dashboard) uchun tayyor agregat ko'rsatkichlar.
/// Faqat o'qish uchun - hech qanday mavjud biznes-mantiqqa ta'sir qilmaydi.
/// </summary>
public record DashboardDto
{
    public decimal TodaySalesAmount { get; init; }
    public int TodaySalesCount { get; init; }
    public decimal TodaySalesLength { get; init; }

    public decimal MonthSalesAmount { get; init; }
    public decimal MonthSalesLength { get; init; }

    public decimal TodayPaymentsAmount { get; init; }
    public int TodayPaymentsCount { get; init; }

    public IReadOnlyList<TopCustomerDto> TopCustomers { get; init; } = [];
    public IReadOnlyList<TopProductDto> TopSellingProducts { get; init; } = [];
    public IReadOnlyList<DailySalesDto> WeeklySales { get; init; } = [];
    public IReadOnlyList<DailySalesDto> WeeklyPayments { get; init; } = [];
}

public record TopCustomerDto
{
    public string Customer { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public record TopProductDto
{
    public string Product { get; init; } = string.Empty;
    public decimal TotalLength { get; init; }
}

public record DailySalesDto
{
    public DateTime Date { get; init; }
    public string Label { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
