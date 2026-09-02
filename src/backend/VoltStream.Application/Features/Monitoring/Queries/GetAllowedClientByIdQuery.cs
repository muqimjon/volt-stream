namespace VoltStream.Application.Features.Monitoring.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Monitoring.DTOs;
using VoltStream.Domain.Entities;
using VoltStream.Application.Commons.Security;

[RequirePermission(Permissions.SettingsUsers)]
public record GetAllowedClientByIdQuery(long Id) : IRequest<AllowedClientDto>;

public class GetAllowedClientByIdQueryHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<GetAllowedClientByIdQuery, AllowedClientDto>
{
    public async Task<AllowedClientDto> Handle(GetAllowedClientByIdQuery request, CancellationToken cancellationToken)
         => mapper.Map<AllowedClientDto>(await context.AllowedClients
             .FirstOrDefaultAsync(ac => ac.Id == request.Id, cancellationToken: cancellationToken))
        ?? throw new NotFoundException(nameof(AllowedClient), nameof(request.Id), request.Id);
}