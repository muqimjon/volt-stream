namespace VoltStream.WebApi.Controllers.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltStream.WebApi.Models;

[AllowAnonymous]
public class HealthController : BaseController
{
    [HttpGet]
    public IActionResult CheckHealth()
        => Ok(new Response { Data = "Server is healthy!" });
}