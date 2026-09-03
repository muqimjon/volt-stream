namespace VoltStream.Application.Features.Payments.Commands;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PaymentsManage)]
public record CreatePaymentCommand(
    DateTimeOffset PaidAt,
    long CustomerId,
    decimal Amount,
    long CurrencyId,
    decimal ExchangeRate,
    decimal NetAmount,
    string Description)
    : IRequest<long>;

public class CreatePaymentCommandHandler(
    IAppDbContext context,
    IMapper mapper)
    : IRequestHandler<CreatePaymentCommand, long>
{
    public async Task<long> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var customer = await context.Customers
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), nameof(request.CustomerId), request.CustomerId);

            var currency = await context.Currencies
                .FirstOrDefaultAsync(c => c.Id == request.CurrencyId, cancellationToken)
                ?? throw new NotFoundException(nameof(Currency), nameof(request.CurrencyId), request.CurrencyId);

            var account = customer.Accounts.FirstOrDefault(a => a.CurrencyId == request.CurrencyId);

            if (account is null)
            {
                account = new Account
                {
                    CustomerId = customer.Id,
                    CurrencyId = request.CurrencyId,
                };

                customer.Accounts.Add(account);
            }


            var cash = currency.IsCash
                ? await EnsureCashExists(currency.Id, cancellationToken)
                : null;

            ApplyBalances(account, currency, cash, request);

            var payment = CreatePayment(request, customer, account, currency);

            context.Payments.Add(payment);

            await context.CommitTransactionAsync(cancellationToken);

            return payment.Id;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Cash> EnsureCashExists(long currencyId, CancellationToken cancellationToken)
    {
        var cash = await context.Cashes
            .FirstOrDefaultAsync(c => c.CurrencyId == currencyId, cancellationToken);

        if (cash is not null) return cash;

        var newCash = new Cash
        {
            CurrencyId = currencyId,
            Balance = 0
        };

        context.Cashes.Add(newCash);
        return newCash;
    }

    private static void ApplyBalances(Account account, Currency currency, Cash? cash, CreatePaymentCommand request)
    {
        account.Balance += request.Amount;
        currency.ExchangeRate = request.ExchangeRate;

        if (currency.IsCash && cash is not null)
            cash.Balance += request.Amount;
    }

    private Payment CreatePayment(CreatePaymentCommand request, Customer customer, Account account, Currency currency)
    {
        var payment = mapper.Map<Payment>(request);
        payment.CurrencyId = currency.Id;
        payment.CustomerId = customer.Id;

        var description = GenerateDescription(request, currency);

        payment.CustomerOperation = new CustomerOperation
        {
            Date = request.PaidAt.UtcDateTime,
            Account = account,
            Amount = request.Amount,
            CustomerId = customer.Id,
            Description = description + ". " + request.Description,
            CreatedAt = DateTime.UtcNow,
            OperationType = OperationType.Payment
        };

        return payment;
    }

    private static string GenerateDescription(CreatePaymentCommand request, Currency currency)
    {
        var typeText = currency.IsCash ? "Naqd" : "O`tkazma";
        return $"{typeText}: {currency.Code}. {request.NetAmount} x {request.ExchangeRate}";
    }
}

