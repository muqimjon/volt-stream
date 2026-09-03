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
    public DbSet<Branch> Branches { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PrintJob> PrintJobs { get; set; }

    private IDbContextTransaction? currentTransaction;
    private int transactionDepth;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is not null)
        {
            transactionDepth++;
            return currentTransaction;
        }

        currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        transactionDepth = 1;
        return currentTransaction;
    }

    public async Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is null)
        {
            await SaveChangesAsync(cancellationToken);
            return true;
        }

        if (transactionDepth > 1)
        {
            transactionDepth--;
            await SaveChangesAsync(cancellationToken);
            return true;
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await currentTransaction.CommitAsync(cancellationToken);
            return true;
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
            transactionDepth = 0;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (currentTransaction is not null)
        {
            await currentTransaction.RollbackAsync(cancellationToken);
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }
        transactionDepth = 0;
    }

    public Task<int> SaveAsync(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);
}
