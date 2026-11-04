namespace DroneInventory.Api.Domain;

// Thrown by domain and service code; DomainExceptionHandler turns them into
// ProblemDetails responses (design.md §5).

/// <summary>Unknown drone, student group or loan → 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Drone already loaned, loan already returned, duplicate name → 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Invalid input such as a blank name or a negative quantity → 422.</summary>
public class ValidationException(string message) : Exception(message);
