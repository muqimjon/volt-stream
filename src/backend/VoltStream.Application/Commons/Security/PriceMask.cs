namespace VoltStream.Application.Commons.Security;

using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Categories.DTOs;
using VoltStream.Application.Features.Customers.DTOs;
using VoltStream.Application.Features.Products.DTOs;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Supplies.DTOs;
using VoltStream.Application.Features.WarehouseStocks.DTOs;

public static class PriceMask
{
    private static readonly string[] blockedKeys =
        ["Amount", "Discount", "UnitPrice", "DiscountRate", "TotalAmount", "FinalAmount", "DiscountAmount"];

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

    public static T ApplyToSupplies<T>(this ICurrentUser user, T supplies) where T : IEnumerable<SupplyDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return supplies;

        foreach (var supply in supplies)
            Hide(supply);

        return supplies;
    }

    public static SupplyDto Apply(this ICurrentUser user, SupplyDto supply)
    {
        if (!user.Has(Permissions.SalesViewPrices))
            Hide(supply);

        return supply;
    }

    public static T ApplyToProducts<T>(this ICurrentUser user, T products) where T : IEnumerable<ProductDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return products;

        foreach (var product in products)
            product.Stocks = null;

        return products;
    }

    public static T ApplyToCategories<T>(this ICurrentUser user, T categories) where T : IEnumerable<CategoryDto>
    {
        if (user.Has(Permissions.SalesViewPrices))
            return categories;

        foreach (var product in categories.SelectMany(category => category.Products ?? []))
            product.Stocks = null;

        return categories;
    }

    public static T ApplyToCustomers<T>(this ICurrentUser user, T customers) where T : IEnumerable<CustomerDto>
    {
        if (user.Has(Permissions.ReportsView))
            return customers;

        foreach (var customer in customers)
            customer.Accounts = null!;

        return customers;
    }

    public static CustomerDto Apply(this ICurrentUser user, CustomerDto customer)
    {
        if (!user.Has(Permissions.ReportsView))
            customer.Accounts = null!;

        return customer;
    }

    public static void Sanitize(this ICurrentUser user, FilteringRequest request)
    {
        if (user.Has(Permissions.SalesViewPrices))
            return;

        foreach (var key in request.Filters?.Keys.Where(IsBlocked).ToList() ?? [])
            request.Filters!.Remove(key);

        foreach (var entry in request.Filters ?? [])
            entry.Value.RemoveAll(value => value.StartsWith("include", StringComparison.OrdinalIgnoreCase));

        if (IsBlocked(request.SortBy))
            request.SortBy = null;
    }

    private static bool IsBlocked(string? name)
        => blockedKeys.Contains(name, StringComparer.OrdinalIgnoreCase);

    private static void Hide(SupplyDto supply)
    {
        supply.UnitPrice = 0;
        supply.DiscountRate = 0;

        if (supply.Product is not null)
            supply.Product.Stocks = null;
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

            if (item.Product is not null)
                item.Product.Stocks = null;
        }
    }
}
