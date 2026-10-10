namespace VoltStream.Application.Features.Branches.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.Branches.DTOs;

public record GetAllBranchesQuery : IRequest<IReadOnlyCollection<BranchDto>>;

public class GetAllBranchesQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllBranchesQuery, IReadOnlyCollection<BranchDto>>
{
    public async Task<IReadOnlyCollection<BranchDto>> Handle(GetAllBranchesQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<BranchDto>>(await context.Branches
            .AsNoTracking()
            .Include(b => b.Warehouse)
            .OrderBy(b => b.Id)
            .ToListAsync(cancellationToken));
}
