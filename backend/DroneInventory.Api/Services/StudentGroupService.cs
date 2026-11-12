using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class StudentGroupService(DroneInventoryDbContext db)
{
    /// <summary>
    /// Creates a student group (REQ-003). Throws ValidationException for an empty name and
    /// ConflictException if the name already exists, ignoring case and spaces.
    /// </summary>
    public async Task<StudentGroup> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var group = new StudentGroup(name);

        if (await db.StudentGroups.AnyAsync(g => g.NormalizedName == group.NormalizedName, cancellationToken))
        {
            throw DuplicateName(group);
        }

        db.StudentGroups.Add(group);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request created the same name between the check and the save.
            throw DuplicateName(group);
        }

        return group;
    }

    /// <summary>All student groups, ordered by name (REQ-003).</summary>
    public async Task<IReadOnlyList<StudentGroup>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.StudentGroups
            .AsNoTracking()
            .OrderBy(g => g.NormalizedName)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Renames a student group (REQ-011). Throws NotFoundException for an unknown id,
    /// ValidationException for an empty name and ConflictException if another group has the name.
    /// </summary>
    public async Task<StudentGroup> RenameAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);
        group.Rename(name);

        // Only other groups count: changing the spelling of the own name is allowed.
        if (await db.StudentGroups.AnyAsync(
                g => g.Id != id && g.NormalizedName == group.NormalizedName, cancellationToken))
        {
            throw DuplicateName(group);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request took the name between the check and the save.
            throw DuplicateName(group);
        }

        return group;
    }

    /// <summary>
    /// Deletes a student group that has no loan (REQ-012). Throws NotFoundException for an unknown
    /// id and ConflictException if the group has a loan, active or returned: loans are never
    /// deleted, so the history stays complete.
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);

        if (await db.Loans.AnyAsync(l => l.StudentGroupId == id, cancellationToken))
        {
            throw HasLoans(group);
        }

        db.StudentGroups.Remove(group);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // A loan for this group was created between the check and the delete;
            // the foreign key Loan -> StudentGroup (Restrict) refused the delete.
            // Any other database error is not about loans and is passed on unchanged.
            throw HasLoans(group);
        }
    }

    private async Task<StudentGroup> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.StudentGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Die Schülergruppe {id} gibt es nicht.");

    private static ConflictException HasLoans(StudentGroup group) =>
        new($"Die Schülergruppe \"{group.Name}\" hat Ausleihen und kann nicht gelöscht werden. Der Verlauf bleibt erhalten.");

    private static ConflictException DuplicateName(StudentGroup group) =>
        new($"Eine Schülergruppe mit dem Namen \"{group.Name}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");
}
