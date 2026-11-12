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

    // Change manage-student-groups: a foreign-key violation is told apart from every other error.

    [Theory]
    [InlineData("")]                      // NO ACTION: SQLITE_CONSTRAINT_FOREIGNKEY (787)
    [InlineData(" ON DELETE RESTRICT")]   // RESTRICT, as Loan -> StudentGroup: reported as 1811
    public void DetectsRealSqliteForeignKeyViolation(string onDelete)
    {
        using var database = new TestDatabase();
        using var context = database.CreateContext();
        context.Database.OpenConnection();
        context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
        context.Database.ExecuteSqlRaw("CREATE TABLE Parents (Id INTEGER PRIMARY KEY)");
        context.Database.ExecuteSqlRaw($"CREATE TABLE Children (ParentId INTEGER NOT NULL REFERENCES Parents (Id){onDelete})");
        context.Database.ExecuteSqlRaw("INSERT INTO Parents VALUES (1)");
        context.Database.ExecuteSqlRaw("INSERT INTO Children VALUES (1)");

        var sqliteError = Assert.Throws<SqliteException>(
            () => context.Database.ExecuteSqlRaw("DELETE FROM Parents WHERE Id = 1"));

        Assert.True(new DbUpdateException("save failed", sqliteError).IsForeignKeyViolation());
    }

    [Fact]
    public void TriggerErrorWithoutForeignKeyIsNotForeignKey()
    {
        var trigger = new SqliteException("SQLite Error 19: 'custom trigger check failed'.", 19, 1811);

        Assert.False(new DbUpdateException("save failed", trigger).IsForeignKeyViolation());
    }

    [Fact]
    public void UniqueViolationIsNotForeignKey()
    {
        var unique = new SqliteException("UNIQUE constraint failed", 19, 2067);

        Assert.False(new DbUpdateException("save failed", unique).IsForeignKeyViolation());
    }

    [Fact]
    public void ExceptionWithoutSqliteErrorIsNotForeignKey()
    {
        Assert.False(new DbUpdateException("save failed").IsForeignKeyViolation());
    }
}
