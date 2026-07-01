namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IAccountApi
{
    [Post("/accounts")]
    Task<Response<long>> CreateAsync([Body] AccountRequest request);

    [Put("/accounts")]
    Task<Response<bool>> UpdateAsync([Body] AccountRequest request);

    [Delete("/accounts/{id}")]
    Task<Response<bool>> DeleteAsync(long id);

    [Get("/accounts/{id}")]
    Task<Response<AccountResponse>> GetByIdAsync(long id);

    [Get("/accounts")]
    Task<Response<List<AccountResponse>>> GetAllAsync();

    [Post("/accounts/filter")]
    Task<Response<List<AccountResponse>>> Filter(FilteringRequest request);
}