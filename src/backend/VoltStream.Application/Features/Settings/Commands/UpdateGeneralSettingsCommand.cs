namespace VoltStream.Application.Features.Settings.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsGeneral)]
public record UpdateGeneralSettingsCommand(bool AllowNegativeStock) : IRequest<bool>;

public class UpdateGeneralSettingsCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateGeneralSettingsCommand, bool>
{
    public async Task<bool> Handle(UpdateGeneralSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await context.AppSettings.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(AppSettings));

        settings.AllowNegativeStock = request.AllowNegativeStock;
        await context.SaveAsync(cancellationToken);

        return true;
    }
}
