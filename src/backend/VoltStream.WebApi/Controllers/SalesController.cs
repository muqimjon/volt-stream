namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Sales.Commands;
using VoltStream.Application.Features.Sales.DTOs;
using VoltStream.Application.Features.Sales.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class SalesController
    : ReadOnlyController<SaleDto,
        GetAllSalesQuery,
        GetSaleByIdQuery>
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSaleCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut]
    public async Task<IActionResult> Update(UpdateSaleCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(SaleFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });

    [HttpPost("items/filter")]
    public async Task<IActionResult> GetItemsFiltered(SaleItemHistoryQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });
}
