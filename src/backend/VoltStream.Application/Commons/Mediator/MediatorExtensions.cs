namespace VoltStream.Application.Commons.Mediator;

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(contract => (contract, type)));

        foreach (var (contract, type) in handlers)
            services.AddTransient(contract, type);

        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(provider => provider.GetRequiredService<IMediator>());

        return services;
    }
}
