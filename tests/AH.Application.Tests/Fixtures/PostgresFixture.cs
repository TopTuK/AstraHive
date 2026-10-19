using AH.Application.Tests.Fixtures;
using AH.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace AH.Application.Tests.Fixtures;

// One real PostgreSQL container per test run; the schema comes from the production
// migrations, so the tests also prove the migrations apply cleanly.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    private DbContextOptions<AppDbContext> _options = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
