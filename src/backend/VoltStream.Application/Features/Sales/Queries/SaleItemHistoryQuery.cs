namespace VoltStream.Application.Features.Sales.Queries;

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Sales.DTOs;

public record SaleItemHistoryQuery : IRequest<IReadOnlyCollection<SaleItemHistoryDto>>
{
    public DateTimeOffset BeginDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public long? CustomerId { get; set; }
    public long? CategoryId { get; set; }
    public long? ProductId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SaleItemHistoryQueryHandler(IAppDbContext context, IPagingMetadataWriter writer)
    : IRequestHandler<SaleItemHistoryQuery, IReadOnlyCollection<SaleItemHistoryDto>>
{
    public async Task<IReadOnlyCollection<SaleItemHistoryDto>> Handle(SaleItemHistoryQuery request, CancellationToken ct)
    {
        var offset = request.BeginDate.Offset;
        var begin = new DateTimeOffset(request.BeginDate.Date, offset).ToUniversalTime();
        var end = new DateTimeOffset(request.EndDate.Date, offset).AddDays(1).ToUniversalTime();

        var query = context.SaleItems
            .Where(i => i.Sale.Date >= begin && i.Sale.Date < end);

        if (request.CustomerId is not null) query = query.Where(i => i.Sale.CustomerId == request.CustomerId);
        if (request.CategoryId is not null) query = query.Where(i => i.Product.CategoryId == request.CategoryId);
        if (request.ProductId is not null) query = query.Where(i => i.ProductId == request.ProductId);

        query = query.OrderByDescending(i => i.Sale.Date);

        var projected = query.Select(i => new SaleItemHistoryDto
        {
            Date = i.Sale.Date,
            Customer = i.Sale.Customer!.Name,
            Category = i.Product.Category.Name,
            Name = i.Product.Name,
            LengthPerRoll = i.LengthPerRoll,
            RollCount = i.RollCount,
            UnitPrice = i.UnitPrice,
            Unit = i.Product.Unit,
            TotalLength = i.TotalLength,
        });

        if (request.Page <= 0 || request.PageSize <= 0)
            return await projected.ToListAsync(ct);

        var total = await projected.CountAsync(ct);
        writer.Write(new PagedListMetadata(total, request.Page, request.PageSize, (int)Math.Ceiling((double)total / request.PageSize)));
        return await projected.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
    }
}
