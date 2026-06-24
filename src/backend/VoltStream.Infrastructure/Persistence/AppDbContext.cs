namespace VoltStream.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Domain.Entities;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<AllowedClient> AllowedClients { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerOperation> CustomerOperations { get; set; }
    public DbSet<Cash> Cashes { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Supply> Supplies { get; set; }
    public DbSet<WarehouseStock> WarehouseStocks { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<User> Users { get; set; }

    private IDbContextTransaction? currentTransaction;

    // AuditInterceptor DI orqali (DependencyInjection.cs) bir marta ro'yxatdan o'tadi.
    // Bu yerda qayta qo'shilsa, har SaveChanges'da ikki marta ishlardi - shuning uchun olib tashlandi.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is not null)
            return currentTransaction;

        currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        return currentTransaction;
    }

    public async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        try
        {
            await SaveChangesAsync(cancellationToken);
            if (currentTransaction is not null)
                await currentTransaction.CommitAsync(cancellationToken);
            isSuccess = true;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (currentTransaction is not null)
            {
                await currentTransaction.DisposeAsync();
                currentTransaction = null;
            }
        }

        return isSuccess;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is not null)
        {
            await currentTransaction.RollbackAsync(cancellationToken);
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }
    }

    public Task<int> SaveAsync(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);
}
