namespace VoltStream.Application.Features.CustomerOperations.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.CustomerOperations.DTOs;

public record GetAllCustomerOperationsQuery : IRequest<IReadOnlyCollection<CustomerOperationDto>>;

public class GetAllCustomerOperationsQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllCustomerOperationsQuery, IReadOnlyCollection<CustomerOperationDto>>
{
    public async Task<IReadOnlyCollection<CustomerOperationDto>> Handle(GetAllCustomerOperationsQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(await context.CustomerOperations
            .AsNoTracking()
            .Include(a => a.Account)
            .ToListAsync(cancellationToken));
}
