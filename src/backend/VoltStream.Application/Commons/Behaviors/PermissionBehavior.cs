namespace VoltStream.Application.Commons.Behaviors;

using MediatR;
using System.Reflection;
using VoltStream.Application.Commons.Exceptions;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Security;

public class PermissionBehavior<TRequest, TResponse>(ICurrentUser user, AuditScope scope)
    : IPipelineBehavior<TRequest, TResponse>
{
    private static readonly RequirePermissionAttribute? required =
        typeof(TRequest).GetCustomAttribute<RequirePermissionAttribute>();

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
    {
        if (scope.RequestDepth == 0 && required is not null && user.IsAuthenticated && !user.Has(required.Permission))
            throw new ForbiddenException($"Ruxsat yetarli emas: {required.Permission}");

        scope.RequestDepth++;
        try
        {
            return await next();
        }
        finally
        {
            scope.RequestDepth--;
        }
    }
}
