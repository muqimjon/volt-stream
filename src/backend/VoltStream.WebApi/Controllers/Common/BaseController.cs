namespace VoltStream.WebApi.Controllers.Common;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Commons.Mediator;

[Route("api/[controller]")]
[ApiController]
public class BaseController : ControllerBase
{
    private IMediator? mediator;
    protected IMediator Mediator
        => mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
}
