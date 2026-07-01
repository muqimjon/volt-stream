namespace VoltStream.WebApi;

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using VoltStream.Application;
using VoltStream.Infrastructure;
using VoltStream.WebApi.Conventions;

public static class DependencyInjection
{
    public static void AddDependencies(this IServiceCollection services, IConfiguration conf)
    {
        services.AddApplicationServices();
        services.AddInfrastructureServices(conf);

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
        if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(opt =>
            {
                opt.WithTheme(ScalarTheme.BluePlanet);
                opt.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                opt.WithTitle("VoltStream API Documentation");
                opt.WithFavicon("favicon.ico");
            });
        }
    }

    public static void UseVoltStreamPipeline(this WebApplication app)
    {
        app.UseStaticFiles();
        app.UseCors(s => s.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
