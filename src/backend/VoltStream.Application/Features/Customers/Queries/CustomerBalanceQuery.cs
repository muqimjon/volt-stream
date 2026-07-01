namespace VoltStream.Application.Features.Customers.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Customers.DTOs;

public record CustomerBalanceQuery : IRequest<IReadOnlyCollection<CustomerBalanceDto>>
{
    public long? CustomerId { get; set; }
    public string? Sign { get; set; }
    public decimal Amount { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CustomerBalanceQueryHandler(
    IAppDbContext context,
    IPagingMetadataWriter writer) : IRequestHandler<CustomerBalanceQuery, IReadOnlyCollection<CustomerBalanceDto>>
{
    internal static IQueryable<CustomerBalanceDto> Project(IAppDbContext context, long? customerId, string? sign, decimal amount, string? type)
    {
        var projected = context.Customers
            .Where(c => customerId == null || c.Id == customerId)
            .Select(c => new
            {
                c.Name,
                c.Phone,
                c.Address,
                Balance = c.Accounts.Sum(a => a.Balance),
                Discount = c.Accounts.Select(a => a.Discount).FirstOrDefault()
            })
            .Select(x => new CustomerBalanceDto
            {
                Customer = x.Name,
                Phone = x.Phone,
                Address = x.Address,
                Discount = x.Discount,
                Debitor = x.Balance < 0 ? -x.Balance : 0,
                Creditor = x.Balance > 0 ? x.Balance : 0
            });

        if (!string.IsNullOrEmpty(sign) && amount > 0)
            projected = sign switch
            {
                ">" => projected.Where(x => (x.Debitor > 0 && x.Debitor > amount) || (x.Creditor > 0 && x.Creditor > amount)),
                ">=" => projected.Where(x => (x.Debitor > 0 && x.Debitor >= amount) || (x.Creditor > 0 && x.Creditor >= amount)),
                "=" => projected.Where(x => x.Debitor == amount || x.Creditor == amount),
                "<" => projected.Where(x => (x.Debitor > 0 && x.Debitor < amount) || (x.Creditor > 0 && x.Creditor < amount)),
                "<=" => projected.Where(x => (x.Debitor > 0 && x.Debitor <= amount) || (x.Creditor > 0 && x.Creditor <= amount)),
                "<>" => projected.Where(x => x.Debitor != amount && x.Creditor != amount),
                _ => projected
            };

        projected = type switch
        {
            "Debitor" => projected.Where(x => x.Debitor > 0),
            "Creditor" => projected.Where(x => x.Creditor > 0),
            _ => projected
        };

        return projected;
    }

    public async Task<IReadOnlyCollection<CustomerBalanceDto>> Handle(CustomerBalanceQuery request, CancellationToken cancellationToken)
    {
        var projected = Project(context, request.CustomerId, request.Sign, request.Amount, request.Type);

        if (request.Page <= 0 || request.PageSize <= 0)
            return await projected.ToListAsync(cancellationToken);

        var total = await projected.CountAsync(cancellationToken);
        writer.Write(new PagedListMetadata(total, request.Page, request.PageSize, (int)Math.Ceiling((double)total / request.PageSize)));

        return await projected
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
    }
}
