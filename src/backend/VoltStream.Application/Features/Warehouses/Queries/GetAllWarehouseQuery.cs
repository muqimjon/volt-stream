namespace VoltStream.Application.Features.Warehouses.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Warehouses.DTOs;

public record GetAllWarehouseQuery : IRequest<IReadOnlyCollection<WarehouseDto>>;


public class GetAllWarehouseQueryHandler(
    IAppDbContext context,
    IMapper mapper) :
    IRequestHandler<GetAllWarehouseQuery, IReadOnlyCollection<WarehouseDto>>
{
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(GetAllWarehouseQuery query, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<WarehouseDto>>(await context.Warehouses.Where(w => !w.IsDeleted).ToListAsync(cancellationToken));
}

