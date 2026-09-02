namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Branches.Commands;
using VoltStream.Application.Features.Branches.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class BranchesController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new Response { Data = await Mediator.Send(new GetAllBranchesQuery()) });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateBranchCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });
}
