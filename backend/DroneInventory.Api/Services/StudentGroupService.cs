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
    /// Deletes a student group that has no loan and no student (REQ-012). Throws NotFoundException
    /// for an unknown id and ConflictException if the group has a loan, active or returned (loans are
    /// never deleted, so the history stays complete), or still has students (they are moved or
    /// removed first, so no student disappears silently).
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);

        if (await db.Loans.AnyAsync(l => l.StudentGroupId == id, cancellationToken))
        {
            throw HasLoans(group);
        }

        if (await HasStudentsAsync(id, cancellationToken))
        {
            throw HasStudents(group);
        }

        db.StudentGroups.Remove(group);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // A loan or a student for this group was created between the check and the delete;
            // the foreign key Loan -> StudentGroup or Student -> StudentGroup (Restrict) refused
            // the delete. Any other database error is passed on unchanged.
            throw await HasStudentsAsync(id, cancellationToken) ? HasStudents(group) : HasLoans(group);
        }
    }

    /// <summary>
    /// Marks a student of the group as responsible, replacing the previous one (REQ-020).
    /// Throws NotFoundException for an unknown group or student and ValidationException if the
    /// student belongs to another group; the responsible student stays unchanged then.
    /// </summary>
    public async Task<StudentGroup> SetResponsibleAsync(int id, int studentId, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
            ?? throw new NotFoundException($"Den Schüler {studentId} gibt es nicht.");

        group.SetResponsible(student);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // The student was removed between the check and the save.
            throw new NotFoundException($"Den Schüler {studentId} gibt es nicht.");
        }

        return group;
    }

    /// <summary>Clears the responsible student (REQ-020); also fine if none was set. NotFoundException for an unknown group.</summary>
    public async Task<StudentGroup> ClearResponsibleAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await FindAsync(id, cancellationToken);
        group.ClearResponsible();
        await db.SaveChangesAsync(cancellationToken);
        return group;
    }

    private Task<bool> HasStudentsAsync(int id, CancellationToken cancellationToken) =>
        db.Students.AnyAsync(s => s.StudentGroupId == id, cancellationToken);

    private async Task<StudentGroup> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.StudentGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Die Schülergruppe {id} gibt es nicht.");

    private static ConflictException HasLoans(StudentGroup group) =>
        new($"Die Schülergruppe \"{group.Name}\" hat Ausleihen und kann nicht gelöscht werden. Der Verlauf bleibt erhalten.");

    private static ConflictException HasStudents(StudentGroup group) =>
        new($"Die Schülergruppe \"{group.Name}\" hat noch Schüler und kann nicht gelöscht werden.");

    private static ConflictException DuplicateName(StudentGroup group) =>
        new($"Eine Schülergruppe mit dem Namen \"{group.Name}\" gibt es bereits (Groß- und Kleinschreibung wird nicht unterschieden).");
}
