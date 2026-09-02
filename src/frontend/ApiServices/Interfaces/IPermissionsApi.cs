namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IPermissionsApi
{
    [Get("/permissions")]
    Task<Response<List<PermissionResponse>>> GetAllAsync();
}
