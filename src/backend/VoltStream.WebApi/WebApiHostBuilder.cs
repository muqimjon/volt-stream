namespace VoltStream.WebApi;

using VoltStream.WebApi.Hubs;
using VoltStream.WebApi.Middlewares;

public static class WebApiHostBuilder
{
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDependencies(builder.Configuration);

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.UseVoltStreamPipeline();
        app.UseOpenApiDocumentation();

        app.MapControllers();
        app.MapHub<PrintHub>("/hubs/print");
        app.MapFallbackToFile("{*path:regex(^(?!(api|hubs|scalar|openapi)(/|$))[^.]*$)}", "index.html").AllowAnonymous();

        return app;
    }
}
