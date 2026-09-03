namespace VoltStream.Application.Commons.Mediator;

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

public sealed class Mediator(IServiceProvider provider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> wrappers = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = (RequestHandlerWrapper<TResponse>)wrappers.GetOrAdd(request.GetType(), type
            => Activator.CreateInstance(typeof(RequestHandlerWrapper<,>).MakeGenericType(type, typeof(TResponse)))!);

        return wrapper.Handle(request, provider, cancellationToken);
    }
}

internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new InvalidOperationException(
                $"IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> ro'yxatdan o'tmagan.");

        var typed = (TRequest)request;
        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typed, cancellationToken);

        return provider.GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .Aggregate(pipeline, (next, behavior) => () => behavior.Handle(typed, cancellationToken, next))();
    }
}
