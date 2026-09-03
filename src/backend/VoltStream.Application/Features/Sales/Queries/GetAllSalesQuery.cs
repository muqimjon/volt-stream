namespace VoltStream.Application.Features.Sales.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Sales.DTOs;

public record GetAllSalesQuery : IRequest<IReadOnlyCollection<SaleDto>>;

public class GetAllSalesQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetAllSalesQuery, IReadOnlyCollection<SaleDto>>
{
    public async Task<IReadOnlyCollection<SaleDto>> Handle(GetAllSalesQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(null, true, cancellationToken);

        var query = context.Sales
            .AsNoTracking()
            .Include(sale => sale.CustomerOperation)
            .AsQueryable();

        if (branchId is not null)
            query = query.Where(sale => sale.BranchId == branchId);

        return user.Apply(mapper.Map<IReadOnlyCollection<SaleDto>>(await query.ToListAsync(cancellationToken)));
    }
}
