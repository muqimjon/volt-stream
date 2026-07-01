namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

[Headers("accept: application/json")]
public interface ISaleApi
{
    [Post("/sales")]
    Task<Response<long>> CreateAsync([Body] SaleRequest request);

    [Put("/sales")]
    Task<Response<bool>> Update([Body] SaleRequest request);

    [Post("/sales/filter")]
    Task<Response<List<SaleResponse>>> Filtering(FilteringRequest request);

    [Post("/sales/items/filter")]
    Task<Response<List<SaleItemHistoryResponse>>> FilterItems(SaleItemHistoryRequest request);

    [Get("/sales")]
    Task<Response<List<SaleResponse>>> GetAll();

    [Get("/sales/{id}")]
    Task<Response<SaleResponse>> GetById(long id);
}
