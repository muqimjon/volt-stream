namespace VoltStream.Application.Features.Currencies.Commands;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;

[RequirePermission(Permissions.SettingsCurrencies)]
public record CreateCurrencyCommand(
    string Name,
    string Code,
    string Symbol,
    bool IsDefault,
    bool IsActive,
    bool IsEditable,
    int Position,
    bool IsCash,
    decimal ExchangeRate)
    : IRequest<long>;

public class CreateCurrencyCommandHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<CreateCurrencyCommand, long>
{
    public async Task<long> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var CurrencyExists = await context.Currencies
            .AnyAsync(p => p.NormalizedName == request.Name.ToNormalized(), cancellationToken);

        if (CurrencyExists)
            throw new AlreadyExistException(nameof(Currency));

        var currency = mapper.Map<Currency>(request);
        context.Currencies.Add(currency);
        await context.SaveAsync(cancellationToken);
        return currency.Id;
    }
}