namespace VoltStream.Application.Features.Customers.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Accounts.DTOs;
using VoltStream.Domain.Entities;

public record CreateCustomerCommand(
    string Name,
    string? Phone,
    string? Address,
    string? Email,
    string? ImageUrl,
    string? ClientType,
    string? Description,
    List<AccountCommandDto> Accounts)
    : IRequest<long>;

public class CreateCustomerComandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<CreateCustomerCommand, long>
{
    public async Task<long> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var isExist = await context.Customers
                    .AnyAsync(user => user.NormalizedName == request.Name.ToNormalized(), cancellationToken);

        if (isExist)
            throw new AlreadyExistException(nameof(Customer), nameof(request.Name), request.Name);
        Currency currency = await GetCurrencyAsync(cancellationToken);

        var customer = mapper.Map<Customer>(request);
        foreach (var account in customer.Accounts.Where(a => a.CurrencyId == 0))
            account.Currency = currency;
        context.Customers.Add(customer);

        await context.SaveAsync(cancellationToken);
        return customer.Id;
    }

    private async Task<Currency> GetCurrencyAsync(CancellationToken cancellationToken)
    {
        var currency = await context.Currencies.FirstOrDefaultAsync(c => c.IsDefault, cancellationToken);
        if (currency is null)
            context.Currencies.Add(currency = new()
            {
                Code = "UZS",
                IsActive = true,
                IsDefault = true,
                Position = 1,
                ExchangeRate = 1,
                IsCash = true,
                IsEditable = false,
                Name = "So'm",
                NormalizedName = "So'm".ToNormalized(),
                Symbol = "so'm",
            });

        return currency;
    }
}