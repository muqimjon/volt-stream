namespace VoltStream.Application.Features.CustomerOperations.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.DTOs;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.ReportsView)]
public record GetCustomerOperationByIdQuery(long Id) : IRequest<CustomerOperationDto>;

public class GetCustomerOperationByIdQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetCustomerOperationByIdQuery, CustomerOperationDto>
{
    public async Task<CustomerOperationDto> Handle(GetCustomerOperationByIdQuery request, CancellationToken cancellationToken)
    {
        var operation = await context.CustomerOperations
            .Include(co => co.Account)
                .ThenInclude(a => a.Customer)
            .Include(co => co.Account)
                .ThenInclude(co => co.Currency)
            .Include(co => co.Sale)
                .ThenInclude(s => s!.Items)
                    .ThenInclude(si => si.Product)
                        .ThenInclude(p => p.Category)
            .Include(co => co.Payment)
                .ThenInclude(p => p!.Currency)
            .FirstOrDefaultAsync(co => co.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOperation), nameof(request.Id), request.Id);

        return user.Apply(mapper.Map<CustomerOperationDto>(operation));
    }
}

