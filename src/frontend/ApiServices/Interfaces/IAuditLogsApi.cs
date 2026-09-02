namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IAuditLogsApi
{
    [Post("/audit-logs/filter")]
    Task<Response<List<AuditLogResponse>>> Filter(FilteringRequest request);
}
