namespace VoltStream.Application.Features.Customers.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Customers.DTOs;

public record GetAllCustomersQuery : IRequest<IReadOnlyCollection<CustomerDto>>;

public class GetAllCustomersQueryHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<GetAllCustomersQuery, IReadOnlyCollection<CustomerDto>>
{
    public async Task<IReadOnlyCollection<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyCollection<CustomerDto>>(await context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .ToListAsync(cancellationToken));
}
