namespace VoltStream.WebApi;

using VoltStream.WebApi.Extensions;
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
        app.UseVoltStreamMiddlewares();

        app.MapControllers();

        return app;
    }
}
