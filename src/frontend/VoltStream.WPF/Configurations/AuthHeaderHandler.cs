namespace VoltStream.WPF.Configurations;

using System.Net.Http;
using System.Net.Http.Headers;
using VoltStream.WPF.Commons.Services;

public class AuthHeaderHandler(ISessionService session, ConnectionRecovery recovery) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authenticated = !string.IsNullOrEmpty(session.Token);
        if (authenticated)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException) when (authenticated)
        {
            _ = Task.Run(recovery.Prompt);
            throw;
        }
        catch (TaskCanceledException) when (authenticated && !cancellationToken.IsCancellationRequested)
        {
            _ = Task.Run(recovery.Prompt);
            throw;
        }
    }
}
