using DroneInventory.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Infrastructure;

// In-memory SQLite database built from the real migrations, so tests use the
// same schema (indexes, constraints) as the application.
// The database lives as long as the connection; dispose to throw it away.
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public DroneInventoryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DroneInventoryDbContext>()
            .UseSqlite(_connection)
            .Options);

    public void Dispose() => _connection.Dispose();
}
