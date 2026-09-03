namespace VoltStream.Application.Features.CustomerOperations.Queries;

using MapsterMapper;
using MediatR;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.CustomerOperations.DTOs;

[RequirePermission(Permissions.ReportsView)]
public record CustomerOperationFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<CustomerOperationDto>>;

public class CustomerOperationFilterQueryHandler(
    IAppDbContext context,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<CustomerOperationFilterQuery, IReadOnlyCollection<CustomerOperationDto>>
{
    public async Task<IReadOnlyCollection<CustomerOperationDto>> Handle(CustomerOperationFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        return user.ApplyToOperations(mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(await context.CustomerOperations
            .ToPagedListAsync(request, writer, cancellationToken)));
    }
}
