namespace VoltStream.Application.Commons.Security;

using VoltStream.Domain.Enums;

public static class RolePresets
{
    private static readonly string[] manager =
        [.. Permissions.All.Where(p => !p.StartsWith("settings.")
            && p != Permissions.BranchesAll
            && p != Permissions.PrintAgent)];

    private static readonly string[] cashier =
    [
        Permissions.SalesView, Permissions.SalesViewPrices, Permissions.PaymentsManage,
        Permissions.CustomersManage, Permissions.StockView, Permissions.ReportsView,
        Permissions.PrintRequest
    ];

    private static readonly string[] seller =
    [
        Permissions.SalesCreate, Permissions.SalesView, Permissions.SuppliesCreate,
        Permissions.SuppliesView, Permissions.StockView, Permissions.CustomersManage,
        Permissions.PrintRequest
    ];

    public static string[] For(UserRole role) => role switch
    {
        UserRole.Admin => Permissions.All,
        UserRole.Manager => manager,
        UserRole.Cashier => cashier,
        _ => seller
    };
}
