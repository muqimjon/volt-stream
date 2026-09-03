namespace VoltStream.Application.Features.Customers.Queries;

using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Customers.DTOs;
using VoltStream.Domain.Entities;

public record GetCustomerByIdQuery(long Id) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IMapper mapper)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), nameof(request.Id), request.Id);

        return user.Apply(mapper.Map<CustomerDto>(customer));
    }
}
