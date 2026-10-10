namespace VoltStream.WPF.Commons.Services;

using ApiServices.Extensions;
using ApiServices.Interfaces;
using ApiServices.Models;
using ApiServices.Models.Responses;

public class SalePolicy(ISettingsApi settingsApi, IWarehouseStocksApi stocksApi)
{
    private HashSet<long>? stockedProducts;
    private HashSet<long>? stockedCategories;

    public bool AllowNegativeStock { get; private set; } = true;

    public async Task LoadAsync(long? branchId)
    {
        var settings = await settingsApi.GetGeneralAsync().Handle();
        AllowNegativeStock = !settings.IsSuccess || settings.Data.AllowNegativeStock;
        stockedProducts = stockedCategories = null;
        if (AllowNegativeStock) return;

        var request = new FilteringRequest
        {
            Filters = new() { ["Product"] = ["include"], ["TotalLength"] = [">0"] },
            BranchId = branchId
        };
        var stocks = await stocksApi.Filter(request).Handle();
        if (!stocks.IsSuccess) return;

        stockedProducts = [.. stocks.Data!.Select(s => s.Product.Id)];
        stockedCategories = [.. stocks.Data!.Select(s => s.Product.CategoryId)];
    }

    // Qoldiq ro'yxati yuklanmasa ro'yxatni yashirmaymiz: ortiqcha savdoni baribir server rad etadi.
    public bool ShowProduct(long productId) => stockedProducts?.Contains(productId) ?? true;

    public bool ShowCategory(long categoryId) => stockedCategories?.Contains(categoryId) ?? true;

    public bool ShowStock(WarehouseStockResponse stock) => AllowNegativeStock || stock.TotalLength > 0;
}
