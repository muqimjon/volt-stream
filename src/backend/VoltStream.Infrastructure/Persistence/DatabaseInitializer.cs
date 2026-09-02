namespace VoltStream.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoltStream.Application.Commons.Security;
using VoltStream.Domain.Entities;
using VoltStream.Domain.Enums;
using VoltStream.Infrastructure.Persistence.Configurations;

public class DatabaseInitializer(
    IServiceProvider services,
    ILogger<DatabaseInitializer> logger) : BackgroundService
{
    private static readonly Dictionary<string, string> dataMigrations = new()
    {
        ["20260902114838_AddCreatedByUserId"] = """UPDATE "Users" SET "IsActive" = true;""",
        ["20260902120211_AddBranches"] = """UPDATE "Users" SET "BranchId" = (SELECT MIN("Id") FROM "Branches") WHERE "Role" <> 0 AND "BranchId" IS NULL;""",
        ["20260902121445_AddUserPermissions"] = PermissionPresetSql()
    };

    private static string PermissionPresetSql()
        => string.Join('\n', Enum.GetValues<UserRole>().Select(role =>
            $"""UPDATE "Users" SET "Permissions" = {PgArray(RolePresets.For(role))} WHERE "Role" = {(int)role} AND cardinality("Permissions") = 0;"""));

    private static string PgArray(string[] values)
        => $"ARRAY[{string.Join(',', values.Select(v => $"'{v}'"))}]::text[]";

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
                await EnsureDefaultBranchAsync(context, stoppingToken);
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

    private static async Task EnsureDefaultBranchAsync(AppDbContext context, CancellationToken ct)
    {
        var branch = await context.Branches.OrderBy(b => b.Id).FirstOrDefaultAsync(ct);

        if (branch is null)
        {
            branch = new Branch
            {
                Name = BranchConfiguration.DefaultBranchName,
                NormalizedName = BranchConfiguration.DefaultBranchName.ToUpperInvariant()
            };
            context.Branches.Add(branch);
            await context.SaveChangesAsync(ct);
        }

        if (!await context.Warehouses.AnyAsync(w => w.BranchId == branch.Id, ct))
        {
            context.Warehouses.Add(new Warehouse
            {
                BranchId = branch.Id,
                Name = branch.Name,
                NormalizedName = branch.NormalizedName
            });
            await context.SaveChangesAsync(ct);
        }
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
