namespace VoltStream.WebApi;

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using VoltStream.Application;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Infrastructure;
using VoltStream.WebApi.Conventions;
using VoltStream.WebApi.Hubs;

public static class DependencyInjection
{
    public static void AddDependencies(this IServiceCollection services, IConfiguration conf)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices(conf);
        services.AddScoped<IPrintJobNotifier, PrintJobNotifier>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });
        services.AddSignalR()
                .AddJsonProtocol(opt => opt.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddControllers(opt => opt.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer())))
                .AddApplicationPart(typeof(DependencyInjection).Assembly)
                .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT tokenni kiriting (masalan: Bearer {token})"
                };
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes.Add("Bearer", securityScheme);

                document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = Array.Empty<string>()
                });

                return Task.CompletedTask;
            });
        });
    }

    public static void UseOpenApiDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
            app.MapScalarApiReference(opt =>
            {
                opt.WithTheme(ScalarTheme.BluePlanet);
                opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                opt.WithTitle("VoltStream API Documentation");
                opt.WithFavicon("favicon.ico");
            }).AllowAnonymous();
        }
    }

    private static readonly string[] NoCacheFiles =
        ["index.html", "ngsw.json", "ngsw-worker.js", "manifest.webmanifest"];

    public static void UseVoltStreamPipeline(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                if (NoCacheFiles.Contains(Path.GetFileName(ctx.File.Name)))
                    ctx.Context.Response.Headers.CacheControl = "no-cache";
            }
        });
        app.UseRouting();
        app.UseRateLimiter();
        app.UseCors(s => s.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
