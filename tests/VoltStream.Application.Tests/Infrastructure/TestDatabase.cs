namespace VoltStream.Application.Tests.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltStream.Application.Commons.Interfaces;
using VoltStream.Application.Commons.Models;
using VoltStream.Application.Features.Currencies.Commands;
using VoltStream.Infrastructure.Persistence;
using VoltStream.Infrastructure.Persistence.Interceptors;

public sealed class TestDatabase : IAsyncDisposable
{
    private TestDatabase(ServiceProvider services) => Services = services;

    public ServiceProvider Services { get; }

    public static async Task<TestDatabase> CreateAsync(string connectionString)
    {
        var database = new TestDatabase(new ServiceCollection()
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
            .BuildServiceProvider());

        await database.Write(context => context.Database.MigrateAsync());

        // CreateCustomerCommand asosiy valyuta topilmasa uni o'zi yaratib qo'yadi — buni testlardan oldin aniq qilib qo'yamiz
        await new Actor(database.Services, TestUser.Admin()).Send(
            new CreateCurrencyCommand("So'm", "UZS", "so'm", true, true, false, 1, true, 1));

        return database;
    }

    public Task<World> NewWorld() => World.CreateAsync(this);

    public async Task<TResult> Read<TResult>(Func<AppDbContext, Task<TResult>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentUserHolder>().User = TestUser.Admin();

        return await query((AppDbContext)scope.ServiceProvider.GetRequiredService<IAppDbContext>());
    }

    public Task Write(Func<AppDbContext, Task> change)
        => Read(async context =>
        {
            await change(context);
            return true;
        });

    public ValueTask DisposeAsync() => Services.DisposeAsync();

    private sealed class NoPagingMetadata : IPagingMetadataWriter
    {
        public void Write(PagedListMetadata metadata)
        {
        }
    }
}
