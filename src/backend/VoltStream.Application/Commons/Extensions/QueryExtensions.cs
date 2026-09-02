namespace VoltStream.Application.Commons.Extensions;

using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using VoltStream.Application.Commons.Models;

public static class QueryExtensions
{
    private static readonly string[] blockedColumns = ["Details"];

    internal static bool IsBlocked(string name)
        => name.Contains("Password", StringComparison.OrdinalIgnoreCase) || blockedColumns.Contains(name);

    #region Filtering

    public static IQueryable<T> AsFilterable<T>(this IQueryable<T> query, FilteringRequest request)
        where T : class
    {
        query = ApplyIncludes(query, request);

        var param = Expression.Parameter(typeof(T), "x");
        var props = typeof(T).GetProperties();

        foreach (var entry in request.Filters ?? [])
        {
            if (entry.Value.All(v => v.StartsWith("include", StringComparison.OrdinalIgnoreCase)))
                continue;

            var prop = props.FirstOrDefault(p => string.Equals(p.Name, entry.Key, StringComparison.OrdinalIgnoreCase));
            if (prop is null || IsBlocked(prop.Name)) continue;

            var member = Expression.Property(param, prop.Name);
            var filterExpr = BuildCombinedCondition(member, entry.Value, prop.PropertyType, request.TimeZone);

            if (filterExpr is not null)
                query = query.Where(Expression.Lambda<Func<T, bool>>(filterExpr, param));
        }

        var searchExpr = BuildGlobalSearchExpression<T>(request.Search, param);
        if (searchExpr is not null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(searchExpr, param));

        return query.AsSortable(request);
    }

    #endregion

    #region Condition Building

    private static Expression? BuildCombinedCondition(Expression member, List<string> values, Type targetType, double? timezone)
    {
        var logic = DetectLogicalOperator(values);
        var conditions = values
            .Where(v => !string.IsNullOrWhiteSpace(v) && !IsLogicalToken(v))
            .Select(v => BuildCondition(member, v, targetType, timezone))
            .Where(c => c is not null)
            .ToList();

        if (conditions.Count == 0) return null;

        Expression expr = conditions[0]!;
        for (int i = 1; i < conditions.Count; i++)
            expr = logic == ExpressionType.AndAlso ? Expression.AndAlso(expr, conditions[i]!) : Expression.OrElse(expr, conditions[i]!);

        return expr;
    }

    private static Expression? BuildCondition(Expression member, string raw, Type targetType, double? timezone)
    {
        string op = "=";
        string value = raw;

        if (raw.StartsWith(">=")) { op = ">="; value = raw[2..]; }
        else if (raw.StartsWith("<=")) { op = "<="; value = raw[2..]; }
        else if (raw.StartsWith('>')) { op = ">"; value = raw[1..]; }
        else if (raw.StartsWith('<')) { op = "<"; value = raw[1..]; }
        else if (raw.StartsWith('=')) { op = "="; value = raw[1..]; }
        else if (raw.StartsWith('!')) { op = "not"; value = raw[1..]; }
        else if (raw.StartsWith("in:", StringComparison.OrdinalIgnoreCase)) { op = "in"; value = raw[3..]; }
        else if (raw.StartsWith("contains:", StringComparison.OrdinalIgnoreCase)) { op = "contains"; value = raw[9..]; }
        else if (raw.StartsWith("starts:", StringComparison.OrdinalIgnoreCase)) { op = "starts"; value = raw[7..]; }
        else if (raw.StartsWith("ends:", StringComparison.OrdinalIgnoreCase)) { op = "ends"; value = raw[5..]; }
        else if (raw.StartsWith("equals:", StringComparison.OrdinalIgnoreCase)) { op = "equals"; value = raw[7..]; }

        if (op == "not")
        {
            var inner = BuildCondition(member, value, targetType, timezone);
            return inner is not null ? Expression.Not(inner) : null;
        }

        if (op == "in")
        {
            var listValues = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Select(v => ConversionHelper.TryConvert(v, targetType))
                .ToList();

            var typedArray = Array.CreateInstance(targetType, listValues.Count);

            for (int i = 0; i < listValues.Count; i++)
                typedArray.SetValue(listValues[i], i);

            var arrayExpr = Expression.Constant(typedArray);

            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(targetType);

            return Expression.Call(containsMethod, arrayExpr, member);
        }

        if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset))
        {
            return BuildDateTimeCondition(member, value, op, targetType, timezone);
        }

        object? converted;
        try { converted = ConversionHelper.TryConvert(value, targetType); }
        catch { return null; }

        if (targetType == typeof(string))
        {
            var constant = Expression.Constant(converted, targetType);
            var memberLower = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            var constLower = Expression.Call(constant, nameof(string.ToLower), Type.EmptyTypes);
            return op switch
            {
                "contains" => Expression.Call(memberLower, nameof(string.Contains), Type.EmptyTypes, constLower),
                "starts" => Expression.Call(memberLower, nameof(string.StartsWith), Type.EmptyTypes, constLower),
                "ends" => Expression.Call(memberLower, nameof(string.EndsWith), Type.EmptyTypes, constLower),
                "equals" => Expression.Call(memberLower, nameof(string.Equals), Type.EmptyTypes, constLower, Expression.Constant(StringComparison.OrdinalIgnoreCase)),
                _ => Expression.Equal(member, constant)
            };
        }

        var underlying = Nullable.GetUnderlyingType(targetType);
        Expression valueConst = underlying is null || converted is null
            ? Expression.Constant(converted, targetType)
            : Expression.Convert(Expression.Constant(converted, underlying), targetType);

        return op switch
        {
            "=" => Expression.Equal(member, valueConst),
            ">" => Expression.GreaterThan(member, valueConst),
            ">=" => Expression.GreaterThanOrEqual(member, valueConst),
            "<" => Expression.LessThan(member, valueConst),
            "<=" => Expression.LessThanOrEqual(member, valueConst),
            _ => null
        };
    }

    private static BinaryExpression BuildDateTimeCondition(Expression member, string value, string op, Type targetType, double? timezoneOffset)
    {
        value = value.Trim();
        DateTimeOffset parsedStart;
        DateTimeOffset parsedEnd;

        TimeSpan offset = timezoneOffset.HasValue
            ? TimeSpan.FromHours(timezoneOffset.Value)
            : TimeSpan.Zero;

        string[] dayFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "yyyy/MM/dd", "yyyy.MM.dd"];
        string[] monthFormats = ["yyyy-MM", "MM.yyyy", "yyyy/MM"];
        string[] yearFormats = ["yyyy"];

        if (DateTimeOffset.TryParseExact(value, dayFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtDay))
        {
            parsedStart = new DateTimeOffset(dtDay.Year, dtDay.Month, dtDay.Day, 0, 0, 0, offset);
            parsedEnd = parsedStart.AddDays(1);
        }
        else if (DateTimeOffset.TryParseExact(value, monthFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtMonth))
        {
            parsedStart = new DateTimeOffset(dtMonth.Year, dtMonth.Month, 1, 0, 0, 0, offset);
            parsedEnd = parsedStart.AddMonths(1);
        }
        else if (DateTimeOffset.TryParseExact(value, yearFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtYear))
        {
            parsedStart = new DateTimeOffset(dtYear.Year, 1, 1, 0, 0, 0, offset);
            parsedEnd = parsedStart.AddYears(1);
        }
        else
        {
            parsedStart = ConversionHelper.ParseFlexibleDateTimeOffset(value);

            if (parsedStart.Offset == TimeSpan.Zero && !value.EndsWith('Z'))
            {
                parsedStart = new DateTimeOffset(parsedStart.DateTime, offset);
            }
            parsedEnd = parsedStart.AddSeconds(1);
        }

        var utcStart = parsedStart.ToUniversalTime();
        var utcEnd = parsedEnd.ToUniversalTime();

        Expression startConst = targetType == typeof(DateTime)
            ? Expression.Constant(utcStart.UtcDateTime, typeof(DateTime))
            : Expression.Constant(utcStart, typeof(DateTimeOffset));

        Expression endConst = targetType == typeof(DateTime)
            ? Expression.Constant(utcEnd.UtcDateTime, typeof(DateTime))
            : Expression.Constant(utcEnd, typeof(DateTimeOffset));

        return op switch
        {
            "<" => Expression.LessThan(member, startConst),
            "<=" => Expression.LessThanOrEqual(member, startConst),
            ">" => Expression.GreaterThanOrEqual(member, endConst),
            ">=" => Expression.GreaterThanOrEqual(member, startConst),
            "=" or "equals" => Expression.AndAlso(
                Expression.GreaterThanOrEqual(member, startConst),
                Expression.LessThan(member, endConst)),
            _ => Expression.AndAlso(
                Expression.GreaterThanOrEqual(member, startConst),
                Expression.LessThan(member, endConst))
        };
    }

    private static ExpressionType DetectLogicalOperator(List<string> values)
    {
        var tokens = values.Select(v => v.Trim().ToLower()).ToList();
        return tokens.Contains("or") || tokens.Contains("||") || tokens.Contains("|") ? ExpressionType.OrElse : ExpressionType.AndAlso;
    }

    private static bool IsLogicalToken(string token)
    {
        var t = token.Trim().ToLower();
        return t is "and" or "&&" or "&" or "or" or "||" or "|";
    }

    #endregion

    #region Global Search

    private static Expression? BuildGlobalSearchExpression<T>(string? search, ParameterExpression param)
    {
        if (string.IsNullOrWhiteSpace(search)) return null;

        var stringProps = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && !IsBlocked(p.Name));
        Expression? expr = null;
        var loweredSearch = search.ToLower();

        foreach (var p in stringProps)
        {
            var member = Expression.Property(param, p.Name);
            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var memberToLower = Expression.Call(member, nameof(string.ToLower), Type.EmptyTypes);
            var contains = Expression.Call(memberToLower, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(loweredSearch));
            var andExpr = Expression.AndAlso(notNull, contains);

            expr = expr is null ? andExpr : Expression.OrElse(expr, andExpr);
        }

        return expr;
    }

    #endregion

    #region Includes

    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, FilteringRequest request) where T : class
    {
        var props = typeof(T).GetProperties();

        foreach (var entry in request.Filters ?? [])
        {
            var prop = props.FirstOrDefault(p => string.Equals(p.Name, entry.Key, StringComparison.OrdinalIgnoreCase));
            if (prop is null) continue;

            foreach (var val in entry.Value)
            {
                if (val.Equals("include", StringComparison.OrdinalIgnoreCase))
                    query = query.Include(prop.Name);
                else if (val.StartsWith("include:", StringComparison.OrdinalIgnoreCase))
                {
                    var segments = val["include:".Length..].Split('.')
                        .Select(s => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..]);
                    query = query.Include($"{prop.Name}.{string.Join('.', segments)}");
                }
            }
        }

        return query;
    }

    #endregion
}