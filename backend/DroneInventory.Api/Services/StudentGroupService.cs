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

    private static ConflictException DuplicateName(StudentGroup group) =>
        new($"A student group with the name \"{group.Name}\" already exists (names are compared ignoring case).");
}
