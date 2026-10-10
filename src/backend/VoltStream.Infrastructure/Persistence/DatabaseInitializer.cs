namespace VoltStream.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoltStream.Application.Commons.Services;
using VoltStream.Domain.Entities;
using VoltStream.Infrastructure.Persistence.Configurations;

public class DatabaseInitializer(
    IServiceProvider services,
    ILogger<DatabaseInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= 5 && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await context.Database.MigrateAsync(stoppingToken);
                await EnsureDefaultBranchAsync(context, stoppingToken);
                await EnsurePerformanceIndexesAsync(context, stoppingToken);

                var seeder = scope.ServiceProvider.GetRequiredService<ExcelDataSeeder>();
                await seeder.SeedAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData"));

                AppReadiness.Ready = true;
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ma'lumotlar bazasini tayyorlashda xatolik (urinish {Attempt}/5)", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private static async Task EnsureDefaultBranchAsync(AppDbContext context, CancellationToken ct)
    {
        if (!await context.Branches.AnyAsync(ct))
        {
            context.Branches.Add(new Branch
            {
                Name = BranchConfiguration.DefaultBranchName,
                NormalizedName = BranchConfiguration.DefaultBranchName.ToUpperInvariant()
            });
            await context.SaveChangesAsync(ct);
        }

        await LinkBranchWarehousesAsync(context, ct);
    }

    // Eski Warehouses.BranchId bu kod ishlashidan oldin migratsiyada o'chadi, shuning uchun
    // filial va bo'sh ombor Id tartibida juftlanadi (ikkalasi bir xil tartibda yaratilgan).
    public static async Task LinkBranchWarehousesAsync(AppDbContext context, CancellationToken ct)
    {
        var branches = await context.Branches
            .Where(b => b.WarehouseId == null)
            .OrderBy(b => b.Id)
            .ToListAsync(ct);

        if (branches.Count > 0)
        {
            var free = await context.Warehouses
                .Where(w => !w.IsDeleted && !w.Branches.Any())
                .OrderBy(w => w.Id)
                .ToListAsync(ct);

            for (var i = 0; i < branches.Count; i++)
            {
                var warehouse = i < free.Count ? free[i] : new Warehouse();

                // v1 dan ko'chgan ombor nomsiz — ro'yxat va tanlagichlarda bo'sh ko'rinmasin.
                if (string.IsNullOrWhiteSpace(warehouse.Name))
                {
                    warehouse.Name = branches[i].Name;
                    warehouse.NormalizedName = branches[i].NormalizedName;
                }

                branches[i].Warehouse = warehouse;
            }

            await context.SaveChangesAsync(ct);
        }

        await context.Sales
            .Where(s => s.WarehouseId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId,
                x => context.Branches.Where(b => b.Id == x.BranchId).Select(b => b.WarehouseId).FirstOrDefault()), ct);

        await context.Supplies
            .Where(s => s.WarehouseId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.WarehouseId,
                x => context.Branches.Where(b => b.Id == x.BranchId).Select(b => b.WarehouseId).FirstOrDefault()), ct);
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
