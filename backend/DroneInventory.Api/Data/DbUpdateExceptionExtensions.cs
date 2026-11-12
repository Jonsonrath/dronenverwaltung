using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

public static class DbUpdateExceptionExtensions
{
    // SQLITE_CONSTRAINT_UNIQUE, see https://sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    // SQLITE_CONSTRAINT_FOREIGNKEY, see https://sqlite.org/rescode.html#constraint_foreignkey
    private const int SqliteConstraintForeignKey = 787;

    // SQLITE_CONSTRAINT_TRIGGER: SQLite reports an ON DELETE RESTRICT foreign key with this code
    // (RESTRICT is checked at once, like a trigger), together with "FOREIGN KEY constraint failed".
    private const int SqliteConstraintTrigger = 1811;

    /// <summary>
    /// True when saving failed because a unique index was violated, e.g. a second
    /// active loan for the same drone or a duplicate name (design.md §4a, §4b).
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique };

    /// <summary>
    /// True when saving failed because a foreign key was violated, e.g. deleting a
    /// student group that a loan still references (REQ-012).
    /// </summary>
    public static bool IsForeignKeyViolation(this DbUpdateException exception) =>
        exception.InnerException switch
        {
            SqliteException { SqliteExtendedErrorCode: SqliteConstraintForeignKey } => true,
            SqliteException { SqliteExtendedErrorCode: SqliteConstraintTrigger } error =>
                error.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal),
            _ => false,
        };
}
