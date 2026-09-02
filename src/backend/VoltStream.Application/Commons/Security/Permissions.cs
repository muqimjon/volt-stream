namespace VoltStream.Application.Commons.Security;

public static class Permissions
{
    public const string SalesCreate = "sales.create";
    public const string SalesView = "sales.view";
    public const string SalesViewPrices = "sales.view_prices";
    public const string SalesEdit = "sales.edit";

    public const string SuppliesCreate = "supplies.create";
    public const string SuppliesView = "supplies.view";
    public const string SuppliesEdit = "supplies.edit";

    public const string StockView = "stock.view";
    public const string PaymentsManage = "payments.manage";
    public const string CustomersManage = "customers.manage";
    public const string ReportsView = "reports.view";

    public const string SettingsCurrencies = "settings.currencies";
    public const string SettingsCategories = "settings.categories";
    public const string SettingsProducts = "settings.products";
    public const string SettingsCustomers = "settings.customers";
    public const string SettingsBranches = "settings.branches";
    public const string SettingsUsers = "settings.users";
    public const string SettingsPrinter = "settings.printer";
    public const string SettingsAudit = "settings.audit";

    public const string PrintRequest = "print.request";
    public const string PrintAgent = "print.agent";
    public const string LogsViewOwn = "logs.view_own";
    public const string BranchesAll = "branches.all";

    public static readonly string[] All =
    [
        SalesCreate, SalesView, SalesViewPrices, SalesEdit,
        SuppliesCreate, SuppliesView, SuppliesEdit,
        StockView, PaymentsManage, CustomersManage, ReportsView,
        SettingsCurrencies, SettingsCategories, SettingsProducts, SettingsCustomers,
        SettingsBranches, SettingsUsers, SettingsPrinter, SettingsAudit,
        PrintRequest, PrintAgent, LogsViewOwn, BranchesAll
    ];
}
