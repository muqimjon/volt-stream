namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VoltStream.Application.Features.PrintJobs.Commands;
using VoltStream.Application.Features.PrintJobs.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class PrintJobsController : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePrintJobCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
        => Ok(new Response { Data = await Mediator.Send(new GetPrintJobByIdQuery(id)) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(PrintJobFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetPendingPrintJobsQuery(branchId)) });

    [HttpPost("{id:long}/claim")]
    public async Task<IActionResult> Claim(long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ClaimPrintJobCommand? command)
        => Ok(new Response { Data = await Mediator.Send((command ?? new ClaimPrintJobCommand(id)) with { Id = id }) });

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id)
        => Ok(new Response { Data = await Mediator.Send(new CompletePrintJobCommand(id)) });

    [HttpPost("{id:long}/fail")]
    public async Task<IActionResult> Fail(long id, [FromBody] FailPrintJobCommand command)
        => Ok(new Response { Data = await Mediator.Send(command with { Id = id }) });

    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectPrintJobCommand? command)
        => Ok(new Response { Data = await Mediator.Send((command ?? new RejectPrintJobCommand(id)) with { Id = id }) });

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
        => Ok(new Response { Data = await Mediator.Send(new CancelPrintJobCommand(id)) });
}
