namespace VoltStream.WebApi.Middlewares;

using System.Diagnostics;
using System.Net;
using VoltStream.WebApi.Models;

public class RequestLoggerMiddleware(RequestDelegate next, Action<RequestLog>? logCallback = null)
{
    public async Task Invoke(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        int statusCode = (int)HttpStatusCode.OK;

        try
        {
            await next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = (int)HttpStatusCode.InternalServerError;
            throw;
        }
        finally
        {
            sw.Stop();

            var log = new RequestLog(
                IpAddress: context.Connection.RemoteIpAddress?.ToString(),
                Path: context.Request.Path,
                Method: context.Request.Method,
                UserAgent: context.Request.Headers.UserAgent.ToString(),
                TimeStamp: DateTime.UtcNow,
                StatusCode: statusCode,
                IsSuccess: statusCode is >= 200 and < 300,
                ElapsedMs: sw.ElapsedMilliseconds
            );

            logCallback?.Invoke(log);
        }
    }
}
