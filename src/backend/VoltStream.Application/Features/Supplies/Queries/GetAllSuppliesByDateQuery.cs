namespace VoltStream.Application.Features.Supplies.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Supplies.DTOs;

public record GetAllSuppliesByDateQuery(
    DateTimeOffset OrerationDate) : IRequest<IReadOnlyCollection<SupplyDto>>;

public class GetAllSuppliesByDateQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetAllSuppliesByDateQuery, IReadOnlyCollection<SupplyDto>>
{
    public async Task<IReadOnlyCollection<SupplyDto>> Handle(GetAllSuppliesByDateQuery request, CancellationToken cancellationToken)
    {
        var startLocal = request.OrerationDate.LocalDateTime.Date;
        var endLocal = startLocal.AddDays(1);

        var startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

        var branchId = await branches.ResolveFilterAsync(null, true, cancellationToken);

        var query = context.Supplies
            .Where(s => !s.IsDeleted)
            .Where(s => s.Date >= startUtc && s.Date < endUtc)
            .Include(s => s.Product)
                .ThenInclude(p => p.Category)
            .AsQueryable();

        if (branchId is not null)
            query = query.Where(s => s.BranchId == branchId);

        return user.ApplyToSupplies(mapper.Map<IReadOnlyCollection<SupplyDto>>(
            await query.ToListAsync(cancellationToken)));
    }
}
