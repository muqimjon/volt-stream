namespace VoltStream.Application.Commons.Security;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public static class PriceMask
{
    public static T Apply<T>(this ICurrentUser user, T sales) where T : IEnumerable<SaleDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return sales;

        foreach (var sale in sales)
            Hide(sale);

        return sales;
    }

    public static SaleDto Apply(this ICurrentUser user, SaleDto sale)
    {
        if (!user.Has(Permissions.SalesViewPrices))
            Hide(sale);

        return sale;
    }

    public static T ApplyToItems<T>(this ICurrentUser user, T items) where T : IEnumerable<SaleItemHistoryDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return items;

        foreach (var item in items)
            item.UnitPrice = 0;

        return items;
    }

    public static T ApplyToStocks<T>(this ICurrentUser user, T stocks) where T : IEnumerable<WarehouseStockDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return stocks;

        foreach (var stock in stocks)
        {
            stock.UnitPrice = 0;
            stock.DiscountRate = 0;
        }

        return stocks;
    }

    private static void Hide(SaleDto sale)
    {
        sale.Amount = 0;
        sale.Discount = 0;

        if (sale.CustomerOperation is not null)
        {
            sale.CustomerOperation.Amount = 0;
            sale.CustomerOperation.Description = string.Empty;
        }

        foreach (var item in sale.Items ?? [])
        {
            item.UnitPrice = 0;
            item.DiscountRate = 0;
            item.DiscountAmount = 0;
            item.TotalAmount = 0;
            item.FinalAmount = 0;
        }
    }
}
