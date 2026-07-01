namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Customers.Commands;
using VoltStream.Application.Features.Customers.DTOs;
using VoltStream.Application.Features.Customers.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class CustomersController
    : CrudController<CustomerDto,
        GetAllCustomersQuery,
        GetCustomerByIdQuery,
        CreateCustomerCommand,
        UpdateCustomerCommand,
        DeleteCustomerCommand>
{
    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(CustomerFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });

    [HttpPost("balances")]
    public async Task<IActionResult> GetBalances(CustomerBalanceQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });

    [HttpPost("balances/summary")]
    public async Task<IActionResult> GetBalancesSummary(CustomerBalanceSummaryQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });
}