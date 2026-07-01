namespace VoltStream.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Infrastructure.Persistence;
using VoltStream.Infrastructure.Persistence.Interceptors;
using VoltStream.Infrastructure.Web;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration conf)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<AuditInterceptor>();
        services.AddScoped<ExcelDataSeeder>();
        services.AddHostedService<DatabaseInitializer>();
        services.AddSingleton<IMarketDataService, Web.MarketDataService>();

        services.AddDbContext<IAppDbContext, AppDbContext>((sp, opt) =>
            opt.UseNpgsql(conf.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("VoltStream.Infrastructure"))
               .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<IPagingMetadataWriter, HttpPagingMetadataWriter>();
        services.AddScoped<IJwtProvider, Authentication.JwtProvider>();

        var secretKey = conf["Jwt:Secret"] ?? "SuperSecretKeyForVoltStreamDasturi2026!";
        var key = Encoding.UTF8.GetBytes(secretKey);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = conf["Jwt:Issuer"] ?? "VoltStream",
                    ValidAudience = conf["Jwt:Audience"] ?? "VoltStreamUser",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        services.AddAuthorization();
    }
}
