namespace VoltStream.Application.Features.CustomerOperations.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
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
        => user.ApplyToOperations(mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(await context.CustomerOperations
            .AsNoTracking()
            .Include(a => a.Account)
            .ToListAsync(cancellationToken)));
}
