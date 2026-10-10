namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IPrintJobsApi
{
    [Post("/print-jobs")]
    Task<Response<PrintJobResponse>> CreateAsync([Body] PrintJobRequest request);

    [Get("/print-jobs/{id}")]
    Task<Response<PrintJobResponse>> GetByIdAsync(long id);

    [Post("/print-jobs/filter")]
    Task<Response<List<PrintJobResponse>>> Filter([Body] FilteringRequest request, [Query] long? branchId = null);

    [Get("/print-jobs/pending")]
    Task<Response<List<PrintJobResponse>>> GetPendingAsync([Query] long? branchId = null);

    [Post("/print-jobs/{id}/claim")]
    Task<Response<PrintJobResponse>> ClaimAsync(long id, [Body] PrintJobClaimRequest request);

    [Post("/print-jobs/{id}/complete")]
    Task<Response<PrintJobResponse>> CompleteAsync(long id);

    [Post("/print-jobs/{id}/fail")]
    Task<Response<PrintJobResponse>> FailAsync(long id, [Body] PrintJobFailRequest request);

    [Post("/print-jobs/{id}/reject")]
    Task<Response<PrintJobResponse>> RejectAsync(long id, [Body] PrintJobRejectRequest request);

    [Post("/print-jobs/{id}/cancel")]
    Task<Response<PrintJobResponse>> CancelAsync(long id);
}
