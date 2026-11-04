using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Data;

public static class DbUpdateExceptionExtensions
{
    // SQLITE_CONSTRAINT_UNIQUE, see https://sqlite.org/rescode.html#constraint_unique
    private const int SqliteConstraintUnique = 2067;

    /// <summary>
    /// True when saving failed because a unique index was violated, e.g. a second
    /// active loan for the same drone or a duplicate name (design.md §4a, §4b).
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteConstraintUnique };
}
