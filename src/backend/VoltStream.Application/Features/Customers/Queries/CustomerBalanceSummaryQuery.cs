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

        var summary = await projected
            .GroupBy(_ => 1)
            .Select(g => new CustomerBalanceSummaryDto
            {
                Discount = g.Sum(x => (decimal?)x.Discount) ?? 0,
                Debitor = g.Sum(x => (decimal?)x.Debitor) ?? 0,
                Creditor = g.Sum(x => (decimal?)x.Creditor) ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary ?? new CustomerBalanceSummaryDto();
    }
}
