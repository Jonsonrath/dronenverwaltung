using DroneInventory.Api.Data;
using DroneInventory.Api.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Tests.Errors;

// Task 1.3: detection of a SQLite unique-constraint violation (used later for
// duplicate names and the one-active-loan index, design.md §4a/§4b).
public class UniqueConstraintViolationTests
{
    [Fact]
    public void DetectsRealSqliteUniqueViolation()
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        context.Database.ExecuteSqlRaw("CREATE TABLE Things (Name TEXT NOT NULL UNIQUE)");
        context.Database.ExecuteSqlRaw("INSERT INTO Things VALUES ('TELLO-07')");

        var sqliteError = Assert.Throws<SqliteException>(
            () => context.Database.ExecuteSqlRaw("INSERT INTO Things VALUES ('TELLO-07')"));

        Assert.True(new DbUpdateException("save failed", sqliteError).IsUniqueConstraintViolation());
    }

    [Fact]
    public void OtherConstraintViolationIsNotUnique()
    {
        // 787 = SQLITE_CONSTRAINT_FOREIGNKEY
        var foreignKey = new SqliteException("FOREIGN KEY constraint failed", 19, 787);

        Assert.False(new DbUpdateException("save failed", foreignKey).IsUniqueConstraintViolation());
    }

    [Fact]
    public void ExceptionWithoutSqliteErrorIsNotUnique()
    {
        Assert.False(new DbUpdateException("save failed").IsUniqueConstraintViolation());
    }
}
