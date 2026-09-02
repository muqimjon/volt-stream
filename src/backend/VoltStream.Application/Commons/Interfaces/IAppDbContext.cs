namespace VoltStream.Application.Commons.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltStream.Domain.Entities;

public interface IAppDbContext
{
    DbSet<AllowedClient> AllowedClients { get; }
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleItem> SaleItems { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerOperation> CustomerOperations { get; }
    DbSet<Cash> Cashes { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Supply> Supplies { get; }
    DbSet<WarehouseStock> WarehouseStocks { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<User> Users { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<Branch> Branches { get; }

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
