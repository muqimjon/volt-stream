namespace VoltStream.Application.Features.Supplies.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Supplies.DTOs;
using VoltStream.Domain.Entities;

public record GetSupplyByIdQuery(long Id) : IRequest<SupplyDto>;

public class GetSupplyByIdQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetSupplyByIdQuery, SupplyDto>
{
    public async Task<SupplyDto> Handle(GetSupplyByIdQuery request, CancellationToken cancellationToken)
    {
        var supply = await context.Supplies
            .Where(s => s.IsDeleted != true)
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Supply), nameof(request.Id), request.Id);

        branches.EnsureAccess(supply.BranchId);

        return user.Apply(mapper.Map<SupplyDto>(supply));
    }
}
