namespace VoltStream.Application.Tests.Infrastructure;

using Npgsql;
using Testcontainers.PostgreSql;

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Database";
}

public sealed class PostgresFixture : IAsyncLifetime
{
    // Production bazasi bilan bir xil major versiya (docker-compose.yml)
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private TestDatabase shared = default!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        shared = await TestDatabase.CreateAsync(container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await shared.DisposeAsync();
        await container.DisposeAsync();
    }

    public Task<World> NewWorld() => shared.NewWorld();

    // Butun jadvallarga tegadigan testlar (masalan, DatabaseInitializer) umumiy bazani buzmasligi uchun o'sha konteynerda alohida toza baza
    public async Task<TestDatabase> NewDatabase()
    {
        var name = $"isolated_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync();
        }

        return await TestDatabase.CreateAsync(
            new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString);
    }
}
