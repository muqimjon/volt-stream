namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.SaleDrafts.Commands;
using VoltStream.Application.Features.SaleDrafts.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class SaleDraftsController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetOpen([FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetOpenSaleDraftsQuery(branchId)) });

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
        => Ok(new Response { Data = await Mediator.Send(new GetSaleDraftByIdQuery(id)) });

    [HttpGet("{id:long}/receipt")]
    public async Task<IActionResult> GetReceipt(long id)
        => Ok(new Response { Data = await Mediator.Send(new GetSaleDraftReceiptQuery(id)) });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDraftCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSaleDraftCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
        => Ok(new Response { Data = await Mediator.Send(new CancelSaleDraftCommand(id)) });
}
