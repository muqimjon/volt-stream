namespace VoltStream.Application.Features.Sales.Queries;

using AutoMapper;
using MediatR;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Sales.DTOs;

public record SaleFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<SaleDto>>
{
    public long? BranchId { get; set; }
}

public class SaleFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<SaleFilterQuery, IReadOnlyCollection<SaleDto>>
{
    public async Task<IReadOnlyCollection<SaleDto>> Handle(SaleFilterQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = branchId is null
            ? context.Sales
            : context.Sales.Where(s => s.BranchId == branchId);

        return user.Apply(mapper.Map<IReadOnlyCollection<SaleDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
