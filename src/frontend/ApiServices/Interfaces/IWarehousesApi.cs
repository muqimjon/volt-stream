namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IWarehousesApi
{
    [Post("/warehouses")]
    Task<Response<long>> CreateAsync(WarehouseRequest request);

    [Put("/warehouses")]
    Task<Response<bool>> UpdateAsync(WarehouseRequest request);

    [Delete("/warehouses/{id}")]
    Task<Response<bool>> DeleteAsync(long id);

    [Get("/warehouses/{id}")]
    Task<Response<WarehouseResponse>> GetByIdAsync(long id);

    [Get("/warehouses")]
    Task<Response<List<WarehouseResponse>>> GetAllAsync();
}
