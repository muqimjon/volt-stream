namespace VoltStream.WebApi.Controllers.Common;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Commons.Mediator;
using VoltStream.WebApi.Models;

public abstract class WriteOnlyController<TResponse, TCreateCommand, TUpdateCommand, TDeleteCommand>
    : BaseController
    where TCreateCommand : IRequest<TResponse>
    where TUpdateCommand : IRequest<TResponse>
    where TDeleteCommand : IRequest<TResponse>
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TCreateCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] TUpdateCommand command)
        => Ok(new Response { Data = await Mediator.Send(command) });

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
        => Ok(new Response
        {
            Data = await Mediator.Send(
            (TDeleteCommand)Activator.CreateInstance(typeof(TDeleteCommand), id)!)
        });
}
