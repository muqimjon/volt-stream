namespace VoltStream.Application.Features.Customers.Queries;

using MapsterMapper;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.Customers.DTOs;

public record CustomerFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<CustomerDto>>;

public class CustomerFilterQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<CustomerFilterQuery, IReadOnlyCollection<CustomerDto>>
{
    public async Task<IReadOnlyCollection<CustomerDto>> Handle(CustomerFilterQuery request, CancellationToken cancellationToken)
    {
        user.SanitizeReports(request);

        return user.ApplyToCustomers(mapper.Map<IReadOnlyCollection<CustomerDto>>(await context.Customers
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
