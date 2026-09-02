namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.AuditLogs.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class AuditLogsController : BaseController
{
    [HttpPost("filter")]
    public async Task<IActionResult> GetFiltered(AuditLogFilterQuery query)
        => Ok(new Response { Data = await Mediator.Send(query) });
}
