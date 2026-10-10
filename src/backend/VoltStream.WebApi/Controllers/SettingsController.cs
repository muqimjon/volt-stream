namespace VoltStream.WebApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Features.Settings.Commands;
using VoltStream.Application.Features.Settings.Queries;
using VoltStream.WebApi.Controllers.Common;
using VoltStream.WebApi.Models;

public class SettingsController : BaseController
{
    [HttpGet("general")]
    public async Task<IActionResult> GetGeneral()
        => Ok(new Response { Data = await Mediator.Send(new GetGeneralSettingsQuery()) });

    [HttpPut("general")]
    public async Task<IActionResult> UpdateGeneral([FromBody] UpdateGeneralSettingsCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });
}
