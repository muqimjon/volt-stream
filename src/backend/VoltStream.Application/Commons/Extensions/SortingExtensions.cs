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
        if (prop is null || QueryExtensions.IsBlocked(prop.Name) || !IsSortable(prop.PropertyType))
        {
            if (typeof(T).GetProperty("Id") is null)
                return query;

            sortBy = "Id";
        }

        return request.Descending
            ? query.OrderByDescendingDynamic(sortBy)
            : query.OrderByDynamic(sortBy);
    }

    private static bool IsSortable(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset);
    }
}
