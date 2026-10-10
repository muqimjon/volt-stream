namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface ISettingsApi
{
    [Get("/settings/general")]
    Task<Response<GeneralSettingsResponse>> GetGeneralAsync();

    [Put("/settings/general")]
    Task<Response<bool>> UpdateGeneralAsync(GeneralSettingsRequest request);
}
