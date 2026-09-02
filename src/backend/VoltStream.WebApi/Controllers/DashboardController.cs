namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Dashboard.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class DashboardController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? begin, [FromQuery] DateTime? end, [FromQuery] long? branchId)
        => Ok(new Response { Data = await Mediator.Send(new GetDashboardQuery(begin, end, branchId)) });

    [HttpGet("market")]
    public async Task<IActionResult> GetMarket()
        => Ok(new Response { Data = await Mediator.Send(new GetMarketDataQuery()) });
}
