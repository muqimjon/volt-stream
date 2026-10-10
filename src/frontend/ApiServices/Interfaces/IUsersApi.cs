namespace ApiServices.Interfaces;

using ApiServices.Models;
using ApiServices.Models.Requests;
using ApiServices.Models.Responses;
using Refit;

[Headers("accept: application/json")]
public interface IUsersApi
{
    [Get("/users")]
    Task<Response<List<UserResponse>>> GetAllAsync();

    [Get("/users/{id}")]
    Task<Response<UserResponse>> GetByIdAsync(long id);

    [Get("/users/me")]
    Task<Response<UserResponse>> GetMeAsync();

    [Post("/users")]
    Task<Response<long>> CreateAsync(UserRequest request);

    [Put("/users")]
    Task<Response<bool>> UpdateAsync(UserRequest request);

    [Put("/users/{id}/password")]
    Task<Response<bool>> UpdatePasswordAsync(long id, PasswordRequest request);

    [Post("/users/filter")]
    Task<Response<List<UserResponse>>> Filter(FilteringRequest request);
}
