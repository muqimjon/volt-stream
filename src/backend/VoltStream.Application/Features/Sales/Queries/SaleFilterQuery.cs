namespace VoltStream.Application.Features.Sales.Queries;

using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Extensions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Mediator;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Commons.Security;
using VoltStream.Application.Commons.Services;
using VoltStream.Application.Features.Sales.DTOs;

public record SaleFilterQuery : FilteringRequest, IRequest<IReadOnlyCollection<SaleDto>>
{
    public long? BranchId { get; set; }
}

public class SaleFilterQueryHandler(
    IAppDbContext context,
    BranchContext branches,
    ICurrentUser user,
    IPagingMetadataWriter writer,
    IMapper mapper) : IRequestHandler<SaleFilterQuery, IReadOnlyCollection<SaleDto>>
{
    public async Task<IReadOnlyCollection<SaleDto>> Handle(SaleFilterQuery request, CancellationToken cancellationToken)
    {
        user.Sanitize(request);

        var branchId = await branches.ResolveFilterAsync(request.BranchId, true, cancellationToken);
        var query = context.Sales.AsQueryable();

        if (branchId is not null)
            query = query.Where(s => s.BranchId == branchId);

        if (!user.Has(Permissions.ReportsView))
            query = query.Where(s => s.CreatedByUserId == user.UserId);

        if (!user.Has(Permissions.SalesViewPrices))
            query = query.Include(s => s.Customer).Include(s => s.Items).ThenInclude(i => i.Product);

        var sales = user.Apply(mapper.Map<IReadOnlyCollection<SaleDto>>(await query
            .ToPagedListAsync(request, writer, cancellationToken)));

        await AttachPrintJobsAsync(sales, cancellationToken);

        return sales;
    }

    private async Task AttachPrintJobsAsync(IReadOnlyCollection<SaleDto> sales, CancellationToken cancellationToken)
    {
        var ids = sales.Select(sale => sale.Id).ToList();

        var jobs = await context.PrintJobs
            .AsNoTracking()
            .Where(job => ids.Contains(job.SaleId))
            .Select(job => new { job.Id, job.SaleId, job.Status, job.Error })
            .ToListAsync(cancellationToken);

        var latest = jobs
            .GroupBy(job => job.SaleId)
            .ToDictionary(group => group.Key, group => group.MaxBy(job => job.Id)!);

        foreach (var sale in sales)
            if (latest.TryGetValue(sale.Id, out var job))
                sale.PrintJob = new SalePrintJobDto { Id = job.Id, Status = job.Status, Error = job.Error };
    }
}
