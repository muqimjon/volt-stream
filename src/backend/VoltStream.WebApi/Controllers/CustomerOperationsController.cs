namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.CustomerOperations.Commands;
using VoltStream.Application.Features.CustomerOperations.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class CustomerOperationsController : BaseController
{
    [HttpDelete("{Id}")]
    public async Task<IActionResult> Delete(long Id)
        => Ok(new Response { Data = await Mediator.Send(new DeleteCustomerOperationCommand(Id)) });

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new Response { Data = await Mediator.Send(new GetAllCustomerOperationsQuery()) });

    [HttpGet("{Id}")]
    public async Task<IActionResult> GetById(long Id)
        => Ok(new Response { Data = await Mediator.Send(new GetCustomerOperationByIdQuery(Id)) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(CustomerOperationFilterQuery query)
      => Ok(new Response { Data = await Mediator.Send(query) });

    [HttpGet("by-customer-id/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(long customerId,
        [FromQuery] DateTime? beginDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] double? timeZone = null)
    {
        return Ok(new Response
        {
            Data = await Mediator.Send(
            new GetCustomerOperationByCustomerIdQuery(customerId, beginDate, endDate, timeZone))
        });
    }
}
