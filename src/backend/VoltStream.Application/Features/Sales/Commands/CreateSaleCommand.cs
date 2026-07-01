namespace VoltStream.Application.Features.Sales.Commands;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;

public record CreateSaleCommand(
    DateTimeOffset Date,
    long? CustomerId,
    long CurrencyId,
    int RollCount,
    decimal Length,
    decimal Amount,
    bool IsDiscountApplied,
    decimal Discount,
    string Description,
    List<SaleItemCommand> Items)
    : IRequest<long>;

public class CreateSaleCommandHandler(
    IAppDbContext context,
    IMapper mapper) : IRequestHandler<CreateSaleCommand, long>
{
    public async Task<long> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var warehouse = await GetWarehouseAsync(cancellationToken);
            var customer = await GetCustomerAsync(request.CustomerId, cancellationToken);

            var descriptionBuilder = new StringBuilder();

            await ProcessSaleItemsAsync(request.Items, warehouse, descriptionBuilder, cancellationToken);
            var sale = mapper.Map<Sale>(request);

            if (customer is not null)
            {
                var account = customer.Accounts.FirstOrDefault(a => a.CurrencyId == request.CurrencyId);

                if (account is null)
                    customer.Accounts.Add(account = new()
                    {
                        CurrencyId = request.CurrencyId,
                        OpeningBalance = sale.Amount
                    });

                UpdateAccountBalance(account, sale.Amount, sale.Discount, request.IsDiscountApplied);
                sale.CustomerOperation = CreateCustomerOperation(sale, account, request.Description, descriptionBuilder, request.IsDiscountApplied);
            }

            context.Sales.Add(sale);
            await context.CommitTransactionAsync(cancellationToken);

            return sale.Id;
        }
        catch
        {
            await context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Warehouse> GetWarehouseAsync(CancellationToken cancellationToken)
    {
        return await context.Warehouses
            .Include(w => w.Stocks)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse));
    }

    private async Task<Customer?> GetCustomerAsync(long? customerId, CancellationToken cancellationToken)
    {
        if (customerId is null)
            return default;
        var customer = await context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(a => a.Id == customerId, cancellationToken);

        return customer;
    }

    private async Task ProcessSaleItemsAsync(
        List<SaleItemCommand> saleItems,
        Warehouse warehouse,
        StringBuilder descriptionBuilder,
        CancellationToken cancellationToken)
    {
        foreach (var item in saleItems)
        {
            var residue = warehouse.Stocks
                .FirstOrDefault(r => r.ProductId == item.ProductId && r.LengthPerRoll == item.LengthPerRoll)
                ?? throw new NotFoundException(nameof(WarehouseStock), nameof(item.Id), item.Id);

            residue.RollCount -= item.RollCount;
            residue.TotalLength -= item.RollCount * item.LengthPerRoll;
            residue.UnitPrice = item.UnitPrice;
            residue.DiscountRate = item.DiscountRate;

            await HandleResidueAsync(item, warehouse, cancellationToken);

            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), nameof(item.Id), item.ProductId);

            descriptionBuilder.Append($"{product.Name} - {item.TotalLength:N2} x {item.UnitPrice:N2} = {item.TotalAmount:N2}");

            if (item.DiscountAmount != 0)
                descriptionBuilder.Append($" [ch: {item.DiscountRate:0.##}% = {item.DiscountAmount:N2}]");

            if (saleItems.IndexOf(item) < saleItems.Count - 1)
                descriptionBuilder.Append(";\n");
        }
    }

    private async Task HandleResidueAsync(SaleItemCommand item, Warehouse warehouse, CancellationToken cancellationToken)
    {
        if (item.LengthPerRoll * item.RollCount != item.TotalLength)
        {
            var detail = item.LengthPerRoll * item.RollCount - item.TotalLength;
            var existStock = await context.WarehouseStocks
                .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.LengthPerRoll == detail, cancellationToken);

            if (existStock is null)
                context.WarehouseStocks.Add(new WarehouseStock
                {
                    RollCount = 1,
                    ProductId = item.ProductId,
                    LengthPerRoll = detail,
                    UnitPrice = item.UnitPrice,
                    TotalLength = detail,
                    Warehouse = warehouse
                });
            else
            {
                existStock.RollCount += 1;
                existStock.UnitPrice = item.UnitPrice;
                existStock.TotalLength += detail;
            }
        }
    }

    private static void UpdateAccountBalance(Account account, decimal amount, decimal discount, bool isApplied)
    {
        account.Balance -= amount;
        if (!isApplied)
            account.Discount += discount;
    }

    private static CustomerOperation CreateCustomerOperation(Sale sale, Account account,
                   string description, StringBuilder descriptionBuilder, bool isApplied)
    {
        if (description.Trim().Length > 0) description = description + ". ";
        return new CustomerOperation
        {
            Date = sale.Date,
            Amount = -sale.Amount,
            IsDiscountApplied = sale.IsDiscountApplied,
            Discount = sale.Discount,
            Account = account,
            AccountId = account.Id,
            CustomerId = sale.CustomerId,
            OperationType = OperationType.Sale,
            Description = $"Savdo: {description}\n" +
                          $"{descriptionBuilder}\n" +
                          $"-------------------------------------\n" +
                          $"Jami: {sale.Amount + (isApplied ? sale.Discount : 0):N2}" +
                          (sale.Discount > 0 ? $"\nChegirma: {sale.Discount:N2} {(isApplied ? "(ayrildi)" : "(saqlandi)")}" : "")
        };
    }
}
