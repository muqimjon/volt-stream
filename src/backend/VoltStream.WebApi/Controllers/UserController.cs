namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class UserController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });
}
