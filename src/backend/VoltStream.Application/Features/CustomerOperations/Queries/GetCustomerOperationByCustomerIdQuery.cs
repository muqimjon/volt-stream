namespace VoltStream.Application.Features.CustomerOperations.Queries;

using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Features.CustomerOperations.DTOs;

public record GetCustomerOperationByCustomerIdQuery(
    long CustomerId,
    DateTime? BeginDate,
    DateTime? EndDate
) : IRequest<CustomerOperationSummaryDto>;

public class GetCustomerOperationByCustomerIdQueryHandler(
    IAppDbContext _context,
    IMapper _mapper)
    : IRequestHandler<GetCustomerOperationByCustomerIdQuery, CustomerOperationSummaryDto>
{
    public async Task<CustomerOperationSummaryDto> Handle(
       GetCustomerOperationByCustomerIdQuery request,
       CancellationToken cancellationToken)
    {
        var beginDate = request.BeginDate.HasValue
      ? DateTime.SpecifyKind(request.BeginDate.Value.Date, DateTimeKind.Utc)
      : (DateTime?)null;

        var endDate = request.EndDate.HasValue
            ? DateTime.SpecifyKind(request.EndDate.Value.Date, DateTimeKind.Utc)
            : (DateTime?)null;

        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == request.CustomerId, cancellationToken);

        if (account == null)
            throw new InvalidOperationException("Hisob topilmadi.");

        var allOperations = _context.CustomerOperations
            .AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.Account.CustomerId == request.CustomerId);

        decimal beginBalance = account.OpeningBalance;
        decimal beforeBeginSum = 0;
        if (beginDate.HasValue)
        {
            beforeBeginSum = await allOperations
                .Where(x => x.Date < beginDate.Value)
                .SumAsync(x => x.Amount, cancellationToken);
        }
        beginBalance += beforeBeginSum;
        decimal endBalance = account.OpeningBalance;

        if (endDate.HasValue)
        {
            var adjustedEndDate = endDate.Value.Date.AddDays(1).AddTicks(-1);

            var beforeEndSum = await allOperations
                .Where(x => x.Date <= adjustedEndDate)
                .SumAsync(x => x.Amount, cancellationToken);

            endBalance += beforeEndSum;
        }

        var filtered = allOperations.AsQueryable();

        if (beginDate.HasValue)
        {
            filtered = filtered.Where(x => x.Date >= beginDate.Value);
        }

        if (endDate.HasValue)
        {
            var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            filtered = filtered.Where(x => x.Date <= endUtc);
        }

        var operations = await filtered
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var operationDtos = _mapper.Map<IReadOnlyCollection<CustomerOperationDto>>(operations);

        return new CustomerOperationSummaryDto
        {
            BeginBalance = beginBalance,
            EndBalance = endBalance,
            Operations = operationDtos
        };
    }
}