namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IBranchesApi
{
    [Post("/branches")]
    Task<Response<long>> CreateAsync(BranchRequest request);

    [Put("/branches")]
    Task<Response<bool>> UpdateAsync(BranchRequest request);

    [Get("/branches")]
    Task<Response<List<BranchResponse>>> GetAllAsync();

    [Post("/branches/filter")]
    Task<Response<List<BranchResponse>>> Filter(FilteringRequest request);
}
