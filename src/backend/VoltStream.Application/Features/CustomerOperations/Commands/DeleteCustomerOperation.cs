namespace VoltStream.Application.Features.CustomerOperations.Commands;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

[RequirePermission(Permissions.PaymentsManage)]
public record DeleteCustomerOperationCommand(long CustomerOperationId) : IRequest<bool>;

public class DeleteCustomerOperationCommandHandler(
    IAppDbContext context,
    BranchContext branches)
    : IRequestHandler<DeleteCustomerOperationCommand, bool>
{
    public async Task<bool> Handle(DeleteCustomerOperationCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var customerOperation = await context.CustomerOperations
                .Include(co => co.Account)
                .Include(co => co.Sale)
                    .ThenInclude(s => s!.Items)
                .Include(co => co.Payment)
                    .ThenInclude(p => p!.Currency)
                .FirstOrDefaultAsync(co => co.Id == request.CustomerOperationId, cancellationToken)
                ?? throw new NotFoundException(nameof(CustomerOperation), nameof(request.CustomerOperationId), request.CustomerOperationId);

            var account = customerOperation.Account
                ?? throw new NotFoundException("Operatsiya bog'langan hisob topilmadi.");

            if (customerOperation.Sale is not null)
                branches.EnsureAccess(customerOperation.Sale.BranchId);

            switch (customerOperation.OperationType)
            {
                case OperationType.Payment:
                    await RevertPaymentAsync(customerOperation, cancellationToken);
                    if (customerOperation.Payment != null)
                        context.Payments.Remove(customerOperation.Payment);
                    break;
                case OperationType.Sale:
                    await RevertSaleAsync(customerOperation, account, cancellationToken);
                    if (customerOperation.Sale != null)
                    {
                        await ReopenDraftAsync(customerOperation.Sale.Id, cancellationToken);
                        context.Sales.Remove(customerOperation.Sale);
                    }
                    break;
                case OperationType.Discount:
                    await RevertDiscountAppliedAsync(customerOperation, account, cancellationToken);
                    break;
                default:
                    throw new ConflictException($"Operatsiya turi {customerOperation.OperationType}ni qaytarish qo'llab-quvvatlanmaydi.");
            }

            context.CustomerOperations.Remove(customerOperation);

            return await context.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task RevertPaymentAsync(CustomerOperation co, CancellationToken cancellationToken)
    {
        var payment = co.Payment
            ?? throw new NotFoundException("Bog'langan to'lov yozuvi topilmadi.");

        if (payment.Currency.IsCash)
        {
            var cash = await context.Cashes
                .FirstOrDefaultAsync(c => c.CurrencyId == payment.CurrencyId, cancellationToken)
                ?? throw new NotFoundException("Kassa yozuvi topilmadi.");

            cash.Balance -= co.Amount;
        }
    }

    // Savdo o'chsa uning ro'yxati yetim qolmasin: qayta Open bo'ladi. Tahrirda (Delete+Create)
    // UpdateSaleCommand uni yangi savdoga bog'lab, yana Confirmed qiladi.
    private Task ReopenDraftAsync(long saleId, CancellationToken cancellationToken)
        => context.SaleDrafts
            .Where(d => d.ConfirmedSaleId == saleId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Status, SaleDraftStatus.Open)
                .SetProperty(d => d.ConfirmedSaleId, (long?)null)
                .SetProperty(d => d.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

    private async Task RevertSaleAsync(CustomerOperation co, Account account, CancellationToken cancellationToken)
    {
        var sale = co.Sale
            ?? throw new NotFoundException("Bog'langan savdo yozuvi topilmadi.");

        if (!sale.IsDiscountApplied)
            account.Discount -= sale.Discount;

        var warehouseId = sale.WarehouseId ?? await branches.WarehouseOfAsync(sale.BranchId, cancellationToken);
        var warehouse = await context.Warehouses
            .Include(w => w.Stocks)
            .FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse));

        foreach (var item in sale.Items)
        {
            var existStock = warehouse.Stocks
                .FirstOrDefault(r => r.ProductId == item.ProductId && r.LengthPerRoll == item.LengthPerRoll);

            if (existStock is not null)
            {
                existStock.RollCount += item.RollCount;
                existStock.TotalLength += item.RollCount * item.LengthPerRoll;
            }

            var residue = item.LengthPerRoll * item.RollCount - item.TotalLength;

            if (residue > 0)
            {
                var residueStock = warehouse.Stocks
                    .FirstOrDefault(i => i.ProductId == item.ProductId && i.LengthPerRoll == residue);

                if (residueStock is not null)
                {
                    residueStock.RollCount -= 1;
                    residueStock.TotalLength -= residue;

                    if (residueStock.RollCount == 0)
                        context.WarehouseStocks.Remove(residueStock);
                }
            }
        }
    }

    private async Task RevertDiscountAppliedAsync(CustomerOperation co, Account account, CancellationToken cancellationToken)
    {
        account.Discount += co.Amount;
    }
}