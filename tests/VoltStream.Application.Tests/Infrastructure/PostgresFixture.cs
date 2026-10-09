namespace VoltStream.Application.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Currencies.Commands;
using VoltStream.Infrastructure.Persistence;
using VoltStream.Infrastructure.Persistence.Interceptors;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Database";
}

public sealed class PostgresFixture : IAsyncLifetime
{
    // Production bazasi bilan bir xil major versiya (docker-compose.yml)
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private ServiceProvider services = default!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        var connectionString = container.GetConnectionString();

        services = new ServiceCollection()
            .AddLogging()
            .AddApplicationServices()
            .AddScoped<CurrentUserHolder>()
            .AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUserHolder>().User)
            .AddScoped<AuditInterceptor>()
            .AddScoped<IAuditLogWriter, AuditLogWriter>()
            .AddScoped<IPagingMetadataWriter, NoPagingMetadata>()
            .AddDbContext<IAppDbContext, AppDbContext>((provider, options) => options
                .UseNpgsql(connectionString, b => b.MigrationsAssembly("VoltStream.Infrastructure"))
                .AddInterceptors(provider.GetRequiredService<AuditInterceptor>()))
            .BuildServiceProvider();

        await using (var scope = services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<CurrentUserHolder>().User = TestUser.Admin();
            await ((AppDbContext)scope.ServiceProvider.GetRequiredService<IAppDbContext>()).Database.MigrateAsync();
        }

        // CreateCustomerCommand asosiy valyuta topilmasa uni o'zi yaratib qo'yadi — buni testlardan oldin aniq qilib qo'yamiz
        await new Actor(services, TestUser.Admin()).Send(
            new CreateCurrencyCommand("So'm", "UZS", "so'm", true, true, false, 1, true, 1));
    }

    public async Task DisposeAsync()
    {
        await services.DisposeAsync();
        await container.DisposeAsync();
    }

    public Task<World> NewWorld() => World.CreateAsync(services);

    private sealed class NoPagingMetadata : IPagingMetadataWriter
    {
        public void Write(PagedListMetadata metadata)
        {
        }
    }
}
