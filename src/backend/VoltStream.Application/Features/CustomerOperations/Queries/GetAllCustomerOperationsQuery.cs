namespace VoltStream.Application.Features.CustomerOperations.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.DTOs;

[RequirePermission(Permissions.ReportsView)]
public record GetAllCustomerOperationsQuery : IRequest<IReadOnlyCollection<CustomerOperationDto>>;

public class GetAllCustomerOperationsQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetAllCustomerOperationsQuery, IReadOnlyCollection<CustomerOperationDto>>
{
    public async Task<IReadOnlyCollection<CustomerOperationDto>> Handle(GetAllCustomerOperationsQuery request, CancellationToken cancellationToken)
    {
        var operations = mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(await context.CustomerOperations
            .AsNoTracking()
            .Include(a => a.Account)
            .ToListAsync(cancellationToken));

        await context.FillBalancesAsync(operations.Accounts(), cancellationToken);
        return user.ApplyToOperations(operations);
    }
}
