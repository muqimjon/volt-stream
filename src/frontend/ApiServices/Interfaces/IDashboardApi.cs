namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Responses;
using Refit;
using System.Threading.Tasks;

[Headers("accept: application/json")]
public interface IDashboardApi
{
    [Get("/dashboard")]
    Task<Response<DashboardResponse>> GetAsync(
        [Query(Format = "yyyy-MM-dd")] DateTime? begin = null,
        [Query(Format = "yyyy-MM-dd")] DateTime? end = null);
}
