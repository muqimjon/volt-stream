namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

public interface ILoginApi
{
    [Post("/user/login")]
    Task<Response<UserResponse>> LoginAsync(LoginRequest request);
}
