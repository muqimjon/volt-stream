namespace VoltStream.Application.Features.Settings.Queries;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Features.Settings.DTOs;

public record GetGeneralSettingsQuery : IRequest<GeneralSettingsDto>;

public class GetGeneralSettingsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetGeneralSettingsQuery, GeneralSettingsDto>
{
    public async Task<GeneralSettingsDto> Handle(GetGeneralSettingsQuery request, CancellationToken cancellationToken)
        => new(await context.AppSettings.AllAsync(s => s.AllowNegativeStock, cancellationToken));
}
