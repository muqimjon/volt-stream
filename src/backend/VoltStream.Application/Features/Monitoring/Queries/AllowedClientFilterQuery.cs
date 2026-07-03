namespace VoltStream.Application.Features.Monitoring.Queries;

using AutoMapper;
using MediatR;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Domain.Entities;

public record AllowedClientFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<AllowedClient>>;

public class AllowedClientFilterQueryHandler(
    IAppDbContext context,
    IPagingMetadataWriter writer,
    IMapper mapper)
    : IRequestHandler<AllowedClientFilterQuery, IReadOnlyCollection<AllowedClient>>
{
    public async Task<IReadOnlyCollection<AllowedClient>> Handle(AllowedClientFilterQuery request, CancellationToken ct)
        => mapper.Map<IReadOnlyCollection<AllowedClient>>(await context.AllowedClients
                 .ToPagedListAsync(request, writer, ct));
}
