namespace VoltStream.Application.Commons.Behaviors;

using MediatR;
using System.Reflection;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;

public class PermissionBehavior<TRequest, TResponse>(ICurrentUser user)
    : IPipelineBehavior<TRequest, TResponse>
{
    private static readonly RequirePermissionAttribute? required =
        typeof(TRequest).GetCustomAttribute<RequirePermissionAttribute>();

    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (required is not null && user.IsAuthenticated && !user.Has(required.Permission))
            throw new ForbiddenException($"Ruxsat yetarli emas: {required.Permission}");

        return next();
    }
}
