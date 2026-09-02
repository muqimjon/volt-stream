namespace VoltStream.WebApi.Controllers.Common;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using VoltStream.WebApi.Models;

public abstract class ReadOnlyController<TEntity, TGetAllQuery, TGetByIdQuery>
    : BaseController
    where TGetAllQuery : IRequest<IReadOnlyCollection<TEntity>>
    where TGetByIdQuery : IRequest<TEntity>
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(new Response { Data = await Mediator.Send(Activator.CreateInstance<TGetAllQuery>()!) });

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
        => Ok(new Response
        {
            Data = await Mediator.Send(
            (TGetByIdQuery)Activator.CreateInstance(typeof(TGetByIdQuery), id)!)
        });
}
