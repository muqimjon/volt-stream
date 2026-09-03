namespace VoltStream.Application.Features.Supplies.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Supplies.DTOs;

public record SupplyFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<SupplyDto>>
{
    public long? BranchId { get; set; }
}

public class SupplyFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<SupplyFilterQuery, IReadOnlyCollection<SupplyDto>>
{
    public async Task<IReadOnlyCollection<SupplyDto>> Handle(SupplyFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = context.Supplies.AsQueryable();

        if (branchId is not null)
            query = query.Where(s => s.BranchId == branchId);

        if (!user.Has(Permissions.SalesViewPrices))
            query = query.Include(s => s.Product).ThenInclude(p => p.Category);

        return user.ApplyToSupplies(mapper.Map<IReadOnlyCollection<SupplyDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
