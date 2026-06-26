namespace VoltStream.Application.Features.Dashboard.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Dashboard.DTOs;

public record GetDashboardQuery : IRequest<DashboardDto>;

public class GetDashboardQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    // O'zbekiston vaqti (UTC+5, yozgi vaqt o'zgarmaydi)
    private static readonly TimeSpan UzOffset = TimeSpan.FromHours(5);

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // Chegaralarni mahalliy O'zbekiston vaqtida (+5) hisoblaymiz (kun/oy/hafta to'g'ri bo'lishi uchun),
        var nowUz = DateTimeOffset.UtcNow.ToOffset(UzOffset);
        var todayStartUz = new DateTimeOffset(nowUz.Year, nowUz.Month, nowUz.Day, 0, 0, 0, UzOffset);
        var todayEndUz = todayStartUz.AddDays(1);
        var monthStartUz = new DateTimeOffset(nowUz.Year, nowUz.Month, 1, 0, 0, 0, UzOffset);
        var weekStartUz = todayStartUz.AddDays(-6);

        // ...lekin PostgreSQL 'timestamptz' faqat UTC (offset 0) qabul qiladi, shuning uchun EF query'lariga UTC beramiz.
        var todayStart = todayStartUz.ToUniversalTime();
        var todayEnd = todayEndUz.ToUniversalTime();
        var monthStart = monthStartUz.ToUniversalTime();
        var weekStart = weekStartUz.ToUniversalTime();

        // --- Bugungi savdo ---
        var todaySales = context.Sales.AsNoTracking().Where(s => s.Date >= todayStart && s.Date < todayEnd);
        var todaySalesAmount = await todaySales.SumAsync(s => s.Amount, cancellationToken);
        var todaySalesCount = await todaySales.CountAsync(cancellationToken);
        var todaySalesLength = await todaySales.SumAsync(s => s.Length, cancellationToken);

        // --- Bu oylik savdo ---
        var monthSales = context.Sales.AsNoTracking().Where(s => s.Date >= monthStart && s.Date < todayEnd);
        var monthSalesAmount = await monthSales.SumAsync(s => s.Amount, cancellationToken);
        var monthSalesLength = await monthSales.SumAsync(s => s.Length, cancellationToken);

        // --- Bugungi to'lovlar ---
        var todayPayments = context.Payments.AsNoTracking().Where(p => p.PaidAt >= todayStart && p.PaidAt < todayEnd);
        var todayPaymentsAmount = await todayPayments.SumAsync(p => p.Amount, cancellationToken);
        var todayPaymentsCount = await todayPayments.CountAsync(cancellationToken);

        // --- TOP 5 mijoz (savdo summasi bo'yicha) ---
        var topCustomerRows = await context.Sales.AsNoTracking()
            .Where(s => s.CustomerId != null)
            .GroupBy(s => s.CustomerId)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(s => s.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topCustomerIds = topCustomerRows.Select(x => x.CustomerId).ToList();
        var customerNames = await context.Customers.AsNoTracking()
            .Where(c => topCustomerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var topCustomers = topCustomerRows
            .Select(r => new TopCustomerDto
            {
                Customer = customerNames.FirstOrDefault(n => n.Id == r.CustomerId)?.Name ?? "—",
                Amount = r.Amount
            })
            .ToList();

        // --- TOP 5 sotilayotgan mahsulot (sotilgan uzunlik bo'yicha) ---
        var topProductRows = await context.SaleItems.AsNoTracking()
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, Length = g.Sum(si => si.TotalLength) })
            .OrderByDescending(x => x.Length)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topProductIds = topProductRows.Select(x => x.ProductId).ToList();
        var productNames = await context.Products.AsNoTracking()
            .Where(p => topProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);

        var topSellingProducts = topProductRows
            .Select(r => new TopProductDto
            {
                Product = productNames.FirstOrDefault(n => n.Id == r.ProductId)?.Name ?? "—",
                TotalLength = r.Length
            })
            .ToList();

        // --- Oxirgi 7 kunlik savdo (kunlik summa) ---
        var weekRows = await context.Sales.AsNoTracking()
            .Where(s => s.Date >= weekStart && s.Date < todayEnd)
            .Select(s => new { s.Date, s.Amount })
            .ToListAsync(cancellationToken);

        var weeklySales = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var dayStart = weekStartUz.AddDays(i);
                var dayEnd = dayStart.AddDays(1);
                return new DailySalesDto
                {
                    Date = dayStart.Date,
                    Amount = weekRows.Where(s => s.Date >= dayStart && s.Date < dayEnd).Sum(s => s.Amount)
                };
            })
            .ToList();

        return new DashboardDto
        {
            TodaySalesAmount = todaySalesAmount,
            TodaySalesCount = todaySalesCount,
            TodaySalesLength = todaySalesLength,
            MonthSalesAmount = monthSalesAmount,
            MonthSalesLength = monthSalesLength,
            TodayPaymentsAmount = todayPaymentsAmount,
            TodayPaymentsCount = todayPaymentsCount,
            TopCustomers = topCustomers,
            TopSellingProducts = topSellingProducts,
            WeeklySales = weeklySales,
        };
    }
}
