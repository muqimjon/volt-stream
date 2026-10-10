namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IWarehouseStocksApi
{
    [Get("/warehouse-items")]
    Task<Response<List<WarehouseStockResponse>>> GetAllWarehouseItemsAsync();

    [Get("/warehouse-items/summary")]
    Task<Response<List<StockSummaryResponse>>> GetSummaryAsync([Query] long? branchId = null);

    [Get("/warehouse-items/productId={id}")]
    Task<Response<List<WarehouseStockResponse>>> GetProductDetailsFromWarehouseAsync(long id, [Query] long? branchId = null);

    [Post("/warehouse-items/filter")]
    Task<Response<List<WarehouseStockResponse>>> Filter(FilteringRequest filter);
}
