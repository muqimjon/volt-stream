namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface ISaleDraftsApi
{
    [Get("/sale-drafts")]
    Task<Response<List<SaleDraftResponse>>> GetOpenAsync([Query] long? branchId = null);

    [Get("/sale-drafts/{id}")]
    Task<Response<SaleDraftResponse>> GetByIdAsync(long id);

    [Get("/sale-drafts/{id}/receipt")]
    Task<Response<ReceiptResponse>> GetReceipt(long id);

    [Post("/sale-drafts")]
    Task<Response<long>> CreateAsync([Body] SaleDraftRequest request);

    [Put("/sale-drafts")]
    Task<Response<bool>> UpdateAsync([Body] SaleDraftRequest request);

    [Post("/sale-drafts/{id}/cancel")]
    Task<Response<bool>> CancelAsync(long id);
}
