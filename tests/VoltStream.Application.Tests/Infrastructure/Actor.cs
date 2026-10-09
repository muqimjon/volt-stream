namespace VoltStream.Application.Tests.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using VoltStream.Application.Commons.Mediator;

public sealed class Actor(IServiceProvider services, TestUser user)
{
    public TestUser User { get; } = user;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentUserHolder>().User = User;

        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
