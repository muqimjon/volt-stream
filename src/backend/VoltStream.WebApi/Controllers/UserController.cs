namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class UserController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPost("verify-admin")]
    public async Task<IActionResult> VerifyAdmin([FromBody] VerifyAdminCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });
}
