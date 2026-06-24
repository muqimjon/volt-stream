namespace VoltStream.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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

    public static async Task UseInfrastructureDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();
        await EnsurePerformanceIndexesAsync(context);

        var seeder = services.GetRequiredService<ExcelDataSeeder>();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData");
        await seeder.SeedAsync(path);
    }

    // Hisobotlar va ro'yxatlar sekinlashishining asosiy sababi - eng ko'p o'sadigan
    // jadvallarda (CustomerOperations, Sales, Payments, Supplies) sana/mijoz ustunlari
    // bo'yicha indeks yo'qligi edi. EF migratsiyasiz ham mavjud bazaga xavfsiz qo'shish
    // uchun idempotent "CREATE INDEX IF NOT EXISTS" ishlatiladi.
    private static async Task EnsurePerformanceIndexesAsync(AppDbContext context)
    {
        const string sql = """
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_AccountId_Date" ON "CustomerOperations" ("AccountId", "Date");
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_CustomerId" ON "CustomerOperations" ("CustomerId");
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_Date" ON "CustomerOperations" ("Date");
            CREATE INDEX IF NOT EXISTS "IX_Sales_Date" ON "Sales" ("Date");
            CREATE INDEX IF NOT EXISTS "IX_Payments_PaidAt" ON "Payments" ("PaidAt");
            CREATE INDEX IF NOT EXISTS "IX_Supplies_Date" ON "Supplies" ("Date");
            """;

        await context.Database.ExecuteSqlRawAsync(sql);
    }
}
