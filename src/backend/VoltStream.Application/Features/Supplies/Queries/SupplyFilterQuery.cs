namespace VoltStream.Application.Features.Supplies.Queries;

using AutoMapper;
using MediatR;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Supplies.DTOs;

public record SupplyFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<SupplyDto>>
{
    public long? BranchId { get; set; }
}

public class SupplyFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<SupplyFilterQuery, IReadOnlyCollection<SupplyDto>>
{
    public async Task<IReadOnlyCollection<SupplyDto>> Handle(SupplyFilterQuery request, CancellationToken cancellationToken)
    {
        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = branchId is null
            ? context.Supplies
            : context.Supplies.Where(s => s.BranchId == branchId);

        return mapper.Map<IReadOnlyCollection<SupplyDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken));
    }
}
