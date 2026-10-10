namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Users.Commands;
using VoltStream.Application.Features.Users.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class UsersController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new Response { Data = await Mediator.Send(new GetAllUsersQuery()) });

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrent()
        => Ok(new Response { Data = await Mediator.Send(new GetCurrentUserQuery()) });

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
        => Ok(new Response { Data = await Mediator.Send(new GetUserByIdQuery(id)) });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> ChangePassword(long id, [FromBody] ChangePasswordCommand command)
        => Ok(new Response { Data = await Mediator.Send(command with { Id = id }) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(UserFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });
}
