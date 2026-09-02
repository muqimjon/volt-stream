namespace VoltStream.Application.Features.Currencies.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsCurrencies)]
public record UpdateCurrencyCommand(
    long Id,
    string Name,
    string Code,
    string Symbol,
    bool IsActive,
    bool IsDefault,
    bool IsEditable,
    int Position,
    bool IsCash,
decimal ExchangeRate)
    : IRequest<bool>;

public class UpdateCurrencyCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<UpdateCurrencyCommand, bool>
{
    public async Task<bool> Handle(UpdateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Currencies.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Currency), nameof(request.Id), request.Id);

        mapper.Map(request, entity);
        return await context.SaveAsync(cancellationToken) > 0;
    }
}
