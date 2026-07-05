namespace VoltStream.Application.Commons.Extensions;

using Microsoft.EntityFrameworkCore;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;

public static class PagingExtensions
{
    public static async Task<IReadOnlyCollection<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        FilteringRequest request,
        IPagingMetadataWriter? writer = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var filtered = query.AsNoTracking().AsFilterable(request);
        var total = await filtered.CountAsync(cancellationToken);

        if (request.Page <= 0 || request.PageSize <= 0)
            return await filtered.ToListAsync(cancellationToken);

        var page = request.Page;
        var pageSize = request.PageSize;

        var items = await filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        writer?.Write(new PagedListMetadata(total, page, pageSize,
            (int)Math.Ceiling((double)total / pageSize)));

        return items;
    }
}
