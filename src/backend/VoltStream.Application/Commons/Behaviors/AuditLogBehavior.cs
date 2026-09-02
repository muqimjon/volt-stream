namespace VoltStream.Application.Commons.Behaviors;

using MediatR;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public class AuditScope
{
    public int Depth { get; set; }
    public int RequestDepth { get; set; }
}

public class AuditLogBehavior<TRequest, TResponse>(
    ICurrentUser user,
    IAuditLogWriter writer,
    AuditScope scope) : IPipelineBehavior<TRequest, TResponse>
{
    private const string Suffix = "Command";

    private static readonly JsonSerializerOptions serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly bool isCommand = typeof(TRequest).Name.EndsWith(Suffix);
    private static readonly string action = typeof(TRequest).Name[..^(isCommand ? Suffix.Length : 0)];
    private static readonly string? entityName = FeatureName(typeof(TRequest).Namespace);
    private static readonly PropertyInfo? idProperty = LongProperty("Id");
    private static readonly PropertyInfo? branchProperty = LongProperty("BranchId");
    private static readonly PropertyInfo? usernameProperty = typeof(TRequest).GetProperty("Username");

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (!isCommand)
            return await next();

        scope.Depth++;

        try
        {
            if (scope.Depth > 1)
                return await next();

            var log = Create(request);

            try
            {
                var response = await next();
                log.Success = true;
                log.UserId ??= Property(response, "Id");
                log.EntityId = response is long id ? id : Property(request, idProperty) ?? Property(response, "Id");
                await writer.WriteAsync(log);
                return response;
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.Error = ex.Message;
                log.EntityId = Property(request, idProperty);
                await writer.WriteAsync(log);
                throw;
            }
        }
        finally
        {
            scope.Depth--;
        }
    }

    private AuditLog Create(TRequest request) => new()
    {
        Timestamp = DateTimeOffset.UtcNow,
        UserId = user.UserId,
        Username = user.Username ?? usernameProperty?.GetValue(request) as string,
        BranchId = Property(request, branchProperty) ?? user.BranchId,
        Action = action,
        EntityName = entityName,
        Details = Details(request),
        Source = user.Source
    };

    private static string? Details(TRequest request)
    {
        try
        {
            var node = JsonSerializer.SerializeToNode(request, serializerOptions);
            Redact(node);
            return node?.ToJsonString();
        }
        catch
        {
            return null;
        }
    }

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject json)
            foreach (var (key, value) in json.ToList())
                if (key.Contains("password", StringComparison.OrdinalIgnoreCase))
                    json[key] = "***";
                else
                    Redact(value);
        else if (node is JsonArray array)
            foreach (var item in array)
                Redact(item);
    }

    private static long? Property(object? source, PropertyInfo? property)
        => property?.GetValue(source) as long?;

    private static long? Property(object? source, string name)
        => source?.GetType().GetProperty(name)?.GetValue(source) as long?;

    private static PropertyInfo? LongProperty(string name)
        => typeof(TRequest).GetProperty(name) is PropertyInfo property
           && (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
            ? property
            : null;

    private static string? FeatureName(string? ns)
    {
        var parts = ns?.Split('.') ?? [];
        var index = Array.IndexOf(parts, "Features");

        return index >= 0 && index + 1 < parts.Length ? parts[index + 1] : null;
    }
}
