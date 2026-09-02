namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Stats.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class StatsController : BaseController
{
    [HttpGet("branch")]
    public async Task<IActionResult> GetBranchStats(
        [FromQuery] DateTimeOffset begin,
        [FromQuery] DateTimeOffset end,
        [FromQuery] long? branchId,
        [FromQuery] long? userId)
        => Ok(new Response { Data = await Mediator.Send(new GetBranchStatsQuery(begin, end, branchId, userId)) });
}
