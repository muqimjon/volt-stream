namespace VoltStream.Application.Features.Payments.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PaymentsManage)]
public record ApplyDiscountCommand(
    DateTimeOffset Date,
    long CustomerId,
    decimal DiscountAmount,
    bool IsCash,
    string Description)
    : IRequest<long>;

public class ApplyDiscountCommandHandler(
    IAppDbContext context)
    : IRequestHandler<ApplyDiscountCommand, long>
{
    public async Task<long> Handle(ApplyDiscountCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var customer = await context.Customers
                .Include(c => c.Accounts)
                    .ThenInclude(a => a.Currency)
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), nameof(request.CustomerId), request.CustomerId);

            var account = customer.Accounts.FirstOrDefault()
                ?? throw new NotFoundException("Mijoz hisobi topilmadi");

            if (request.DiscountAmount <= 0)
                throw new AppException("Chegirma summasi 0 dan katta bo'lishi kerak");

            if (account.Discount < request.DiscountAmount)
                throw new ForbiddenException($"Yetarli chegirma yo'q. Mavjud: {account.Discount}, So'ralgan: {request.DiscountAmount}");

            account.Discount -= request.DiscountAmount;

            var customerOperation = new CustomerOperation
            {
                Date = request.Date.ToUniversalTime(),
                AccountId = account.Id,
                Amount = request.DiscountAmount,
                CustomerId = customer.Id,
                Discount = request.DiscountAmount * -1,
                IsDiscountApplied = true,
                Description = $"Chegirma hisobga olindi. {request.Description}",
                OperationType = OperationType.Discount
            };

            context.CustomerOperations.Add(customerOperation);

            if (request.IsCash)
            {
                var cash = await context.Cashes
                    .FirstOrDefaultAsync(c => c.CurrencyId == account.CurrencyId && c.IsActive, cancellationToken)
                    ?? throw new NotFoundException("Faol kassa topilmadi");

                if (cash.Balance < request.DiscountAmount)
                    throw new ForbiddenException($"Kassada yetarli mablag' yo'q. Mavjud: {cash.Balance}, Kerak: {request.DiscountAmount}");

                cash.Balance -= request.DiscountAmount;

                var customerOperationCash = new CustomerOperation
                {
                    Date = request.Date.ToUniversalTime(),
                    AccountId = account.Id,
                    Amount = request.DiscountAmount * -1,
                    CustomerId = customer.Id,
                    Description = $"Chegirma naqd berildi. {request.Description}",
                    OperationType = OperationType.Payment
                };

                context.CustomerOperations.Add(customerOperationCash);

                var paymentOperation = new Payment
                {
                    Description = $"Chegirma naqd berildi. {request.Description}",
                    Amount = request.DiscountAmount * -1,
                    ExchangeRate = 1,
                    NetAmount = request.DiscountAmount * -1,
                    CurrencyId = account.CurrencyId,
                    CustomerId = customer.Id,
                    CustomerOperation = customerOperationCash,
                    PaidAt = request.Date.ToUniversalTime(),
                    Type = PaymentType.Cash,
                };
                context.Payments.Add(paymentOperation);
            }
            else
            {
                account.Balance += request.DiscountAmount;
            }


            await context.CommitTransactionAsync(cancellationToken);
            return customerOperation.Id;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}