namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Dashboard.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class DashboardController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(new Response { Data = await Mediator.Send(new GetDashboardQuery()) });

    [HttpGet("market")]
    public async Task<IActionResult> GetMarket()
        => Ok(new Response { Data = await Mediator.Send(new GetMarketDataQuery()) });
}
