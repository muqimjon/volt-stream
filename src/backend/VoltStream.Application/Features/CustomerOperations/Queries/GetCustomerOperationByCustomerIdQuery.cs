namespace VoltStream.Application.Features.CustomerOperations.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Features.CustomerOperations.DTOs;

[RequirePermission(Permissions.ReportsView)]
public record GetCustomerOperationByCustomerIdQuery(
    long CustomerId,
    DateTime? BeginDate,
    DateTime? EndDate,
    double? TimeZone
) : IRequest<CustomerOperationSummaryDto>;

public class GetCustomerOperationByCustomerIdQueryHandler(
    IAppDbContext _context,
    ICurrentUser user,
    IMapper _mapper)
    : IRequestHandler<GetCustomerOperationByCustomerIdQuery, CustomerOperationSummaryDto>
{
    public async Task<CustomerOperationSummaryDto> Handle(
       GetCustomerOperationByCustomerIdQuery request,
       CancellationToken cancellationToken)
    {
        var offset = TimeSpan.FromHours(request.TimeZone ?? 0);

        var beginDate = request.BeginDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.BeginDate.Value.Date, DateTimeKind.Unspecified), offset).ToUniversalTime()
            : (DateTimeOffset?)null;

        var endDate = request.EndDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Unspecified), offset).AddDays(1).ToUniversalTime()
            : (DateTimeOffset?)null;

        var accounts = _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId);

        if (!await accounts.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Hisob topilmadi.");

        var openingBalance = await accounts.SumAsync(a => a.OpeningBalance, cancellationToken);

        var allOperations = _context.CustomerOperations
            .AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Account.CustomerId == request.CustomerId);

        decimal beginBalance = openingBalance;
        if (beginDate.HasValue)
        {
            beginBalance += await allOperations
                .Where(x => x.Date < beginDate.Value)
                .SumAsync(x => x.Amount, cancellationToken);
        }

        decimal endBalance = openingBalance + await (endDate.HasValue
            ? allOperations.Where(x => x.Date < endDate.Value)
            : allOperations).SumAsync(x => x.Amount, cancellationToken);

        var filtered = allOperations.AsQueryable();

        if (beginDate.HasValue)
            filtered = filtered.Where(x => x.Date >= beginDate.Value);

        if (endDate.HasValue)
            filtered = filtered.Where(x => x.Date < endDate.Value);

        var operations = await filtered
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var operationDtos = _mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(operations);

        return user.Apply(new CustomerOperationSummaryDto
        {
            BeginBalance = beginBalance,
            EndBalance = endBalance,
            Operations = operationDtos
        });
    }
}