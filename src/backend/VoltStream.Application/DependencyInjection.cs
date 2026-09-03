namespace VoltStream.Application;

using Mapster;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.Application.Commons.Behaviors;
using VoltStream.Application.Commons.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(assembly);

        var mapperConfig = new TypeAdapterConfig();
        mapperConfig.Default.PreserveReference(true);
        mapperConfig.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        mapperConfig.Scan(assembly);
        services.AddSingleton(mapperConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<BranchContext>();
        services.AddScoped<AuditScope>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PermissionBehavior<,>));

        return services;
    }
}
