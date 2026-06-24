namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Responses;
using Refit;
using System.Threading.Tasks;

[Headers("accept: application/json")]
public interface IMarketDataApi
{
    [Get("/dashboard/market")]
    Task<Response<MarketDataResponse>> GetAsync();
}
