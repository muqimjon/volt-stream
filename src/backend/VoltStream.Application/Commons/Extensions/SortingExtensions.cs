namespace VoltStream.Application.Commons.Extensions;

using VoltStream.Application.Commons.Models;

public static class SortingExtensions
{
    public static IQueryable<T> AsSortable<T>(this IQueryable<T> query, SortingRequest request)
    {
        var sortBy = string.IsNullOrWhiteSpace(request.SortBy)
            ? "Id"
            : request.SortBy;

        var prop = typeof(T).GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, sortBy, StringComparison.OrdinalIgnoreCase));
        if (prop is null || QueryExtensions.IsBlocked(prop.Name))
            return query;

        return request.Descending
            ? query.OrderByDescendingDynamic(sortBy)
            : query.OrderByDynamic(sortBy);
    }
}
