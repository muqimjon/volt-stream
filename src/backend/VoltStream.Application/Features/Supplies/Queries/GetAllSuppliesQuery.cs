namespace VoltStream.Application.Features.Supplies.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Supplies.DTOs;

public record GetAllSuppliesQuery : IRequest<IReadOnlyCollection<SupplyDto>>;

public class GetAllSuppliesQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetAllSuppliesQuery, IReadOnlyCollection<SupplyDto>>
{
    public async Task<IReadOnlyCollection<SupplyDto>> Handle(GetAllSuppliesQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(null, true, cancellationToken);

        var query = context.Supplies
            .AsNoTracking()
            .Where(supply => supply.IsDeleted != true)
            .Include(supply => supply.Product)
                .ThenInclude(product => product.Category)
            .AsQueryable();

        if (branchId is not null)
            query = query.Where(supply => supply.BranchId == branchId);

        return user.ApplyToSupplies(mapper.Map<IReadOnlyCollection<SupplyDto>>(await query.ToListAsync(cancellationToken)));
    }
}
