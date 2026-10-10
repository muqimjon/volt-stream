namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.WarehouseStocks.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class WarehouseItemsController : BaseController
{

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetAllWarehouseStockQuery(branchId)) });

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetStockSummaryQuery(branchId)) });

    [HttpGet("productId={Id:long}")]
    public async Task<IActionResult> GetAllWarehouseItemByProductId(long Id, [FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetProductDetailsFromWarehouse(Id, branchId)) });

    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(WarehouseStockFilterQuery query)
            => Ok(new Response { Data = await Mediator.Send(query) });
}
