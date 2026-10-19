using AH.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Tests.Fixtures;

// Every test starts from empty tables. Database test classes share one container,
// so they must stay in a single xUnit collection (see DatabaseCollection).
[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTestBase(PostgresFixture database) : IAsyncLifetime
{
    protected AppDbContext CreateDbContext() => database.CreateDbContext();

    public async ValueTask InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE users RESTART IDENTITY CASCADE;");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseCollection
{
    public const string Name = "Database";
}
