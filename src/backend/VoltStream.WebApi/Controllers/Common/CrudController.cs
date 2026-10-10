namespace VoltStream.WebApi.Controllers.Common;

using Microsoft.AspNetCore.Mvc;
using VoltStream.Application.Commons.Mediator;
using VoltStream.WebApi.Models;

public abstract class CrudController<TResponse, TGetAllQuery, TGetByIdQuery,
                                     TCreateCommand, TUpdateCommand, TDeleteCommand>
    : ReadOnlyController<TResponse, TGetAllQuery, TGetByIdQuery>
      where TGetAllQuery : IRequest<IReadOnlyCollection<TResponse>>
      where TGetByIdQuery : IRequest<TResponse>
      where TCreateCommand : IRequest<long>
      where TUpdateCommand : IRequest<bool>
      where TDeleteCommand : IRequest<bool>
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
