using DroneInventory.Api.Data;
using DroneInventory.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DroneInventory.Api.Services;

public class StudentService(DroneInventoryDbContext db)
{
    /// <summary>
    /// Adds a student to a group (REQ-016). Throws NotFoundException for an unknown group,
    /// ValidationException for an empty name and ConflictException if the group already has
    /// a student with that name, ignoring case and spaces.
    /// </summary>
    public async Task<Student> AddAsync(
        int studentGroupId, string name, string? @class, CancellationToken cancellationToken = default)
    {
        var group = await FindGroupAsync(studentGroupId, cancellationToken);
        var student = new Student(group.Id, name, @class);

        if (await NameTakenAsync(group.Id, student, cancellationToken))
        {
            throw DuplicateName(group, student);
        }

        db.Students.Add(student);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request added the same name between the check and the save.
            throw DuplicateName(group, student);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // The group was deleted between the check and the save.
            throw GroupNotFound(studentGroupId);
        }

        return student;
    }

    /// <summary>
    /// Moves a student to another group (REQ-018). Throws NotFoundException for an unknown student
    /// or group and ConflictException if the target group already has a student with that name.
    /// </summary>
    public async Task<Student> MoveAsync(int id, int studentGroupId, CancellationToken cancellationToken = default)
    {
        var student = await FindAsync(id, cancellationToken);
        var group = await FindGroupAsync(studentGroupId, cancellationToken);

        if (await NameTakenAsync(group.Id, student, cancellationToken))
        {
            throw DuplicateName(group, student);
        }

        student.MoveTo(group.Id);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueConstraintViolation())
        {
            // Another request added the same name to the target group between the check and the save.
            throw DuplicateName(group, student);
        }
        catch (DbUpdateException exception) when (exception.IsForeignKeyViolation())
        {
            // The target group was deleted between the check and the save.
            throw GroupNotFound(studentGroupId);
        }

        return student;
    }

    /// <summary>Removes a student (REQ-019). Loans are not affected. Throws NotFoundException for an unknown id.</summary>
    public async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        var student = await FindAsync(id, cancellationToken);
        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>All students grouped by their group id, each group ordered by name (REQ-017).</summary>
    public async Task<ILookup<int, Student>> ListByGroupAsync(CancellationToken cancellationToken = default)
    {
        var students = await db.Students
            .AsNoTracking()
            .OrderBy(s => s.NormalizedName)
            .ToListAsync(cancellationToken);
        return students.ToLookup(s => s.StudentGroupId);
    }

    private Task<bool> NameTakenAsync(int studentGroupId, Student student, CancellationToken cancellationToken) =>
        db.Students.AnyAsync(
            s => s.StudentGroupId == studentGroupId && s.Id != student.Id && s.NormalizedName == student.NormalizedName,
            cancellationToken);

    private async Task<Student> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Den Schüler {id} gibt es nicht.");

    private async Task<StudentGroup> FindGroupAsync(int id, CancellationToken cancellationToken) =>
        await db.StudentGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw GroupNotFound(id);

    private static NotFoundException GroupNotFound(int id) => new($"Die Schülergruppe {id} gibt es nicht.");

    private static ConflictException DuplicateName(StudentGroup group, Student student) =>
        new($"In der Gruppe \"{group.Name}\" gibt es bereits einen Schüler \"{student.Name}\".");
}
