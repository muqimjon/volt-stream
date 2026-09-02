namespace VoltStream.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class DatabaseInitializer(
    IServiceProvider services,
    ILogger<DatabaseInitializer> logger) : BackgroundService
{
    private static readonly Dictionary<string, string> dataMigrations = new()
    {
        ["20260902114838_AddCreatedByUserId"] = """UPDATE "Users" SET "IsActive" = true;"""
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= 5 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pending = await context.Database.GetPendingMigrationsAsync(stoppingToken);
                await context.Database.MigrateAsync(stoppingToken);
                await ApplyDataMigrationsAsync(context, pending, stoppingToken);
                await EnsurePerformanceIndexesAsync(context, stoppingToken);

                var seeder = scope.ServiceProvider.GetRequiredService<ExcelDataSeeder>();
                await seeder.SeedAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData"));
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ma'lumotlar bazasini tayyorlashda xatolik (urinish {Attempt}/5)", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private static async Task ApplyDataMigrationsAsync(AppDbContext context, IEnumerable<string> applied, CancellationToken ct)
    {
        foreach (var id in applied)
            if (dataMigrations.TryGetValue(id, out var sql))
                await context.Database.ExecuteSqlRawAsync(sql, ct);
    }

    private static Task EnsurePerformanceIndexesAsync(AppDbContext context, CancellationToken ct)
    {
        const string sql = """
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_AccountId_Date" ON "CustomerOperations" ("AccountId", "Date");
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_CustomerId" ON "CustomerOperations" ("CustomerId");
            CREATE INDEX IF NOT EXISTS "IX_CustomerOperations_Date" ON "CustomerOperations" ("Date");
            CREATE INDEX IF NOT EXISTS "IX_Sales_Date" ON "Sales" ("Date");
            CREATE INDEX IF NOT EXISTS "IX_Payments_PaidAt" ON "Payments" ("PaidAt");
            CREATE INDEX IF NOT EXISTS "IX_Supplies_Date" ON "Supplies" ("Date");
            """;

        return context.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
