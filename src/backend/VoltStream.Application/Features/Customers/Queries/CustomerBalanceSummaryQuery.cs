namespace VoltStream.Application.Features.Customers.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.Customers.DTOs;

public record CustomerBalanceSummaryQuery : IRequest<CustomerBalanceSummaryDto>
{
    public long? CustomerId { get; set; }
    public string? Sign { get; set; }
    public decimal Amount { get; set; }
    public string? Type { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CustomerBalanceSummaryQueryHandler(
    IAppDbContext context) : IRequestHandler<CustomerBalanceSummaryQuery, CustomerBalanceSummaryDto>
{
    public async Task<CustomerBalanceSummaryDto> Handle(CustomerBalanceSummaryQuery request, CancellationToken cancellationToken)
    {
        var projected = CustomerBalanceQueryHandler.Project(context, request.CustomerId, request.Sign, request.Amount, request.Type);

        return new CustomerBalanceSummaryDto
        {
            Discount = await projected.SumAsync(x => (decimal?)x.Discount, cancellationToken) ?? 0,
            Debitor = await projected.SumAsync(x => (decimal?)x.Debitor, cancellationToken) ?? 0,
            Creditor = await projected.SumAsync(x => (decimal?)x.Creditor, cancellationToken) ?? 0
        };
    }
}
