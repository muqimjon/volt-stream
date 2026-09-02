namespace VoltStream.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Infrastructure.Authentication;
using VoltStream.Infrastructure.Persistence;
using VoltStream.Infrastructure.Persistence.Interceptors;
using VoltStream.Infrastructure.Web;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration conf)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<ExcelDataSeeder>();
        services.AddHostedService<DatabaseInitializer>();
        services.AddSingleton<IMarketDataService, Web.MarketDataService>();

        services.AddDbContext<IAppDbContext, AppDbContext>((sp, opt) =>
            opt.UseNpgsql(conf.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("VoltStream.Infrastructure"))
               .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<IPagingMetadataWriter, HttpPagingMetadataWriter>();
        services.AddScoped<IJwtProvider, JwtProvider>();

        var key = JwtSecretResolver.Resolve(conf);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = conf["Jwt:Issuer"] ?? "VoltStream",
                    ValidAudience = conf["Jwt:Audience"] ?? "VoltStreamUser",
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "unique_name"
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var token = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            ctx.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(o =>
            o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }
}
