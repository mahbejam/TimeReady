using TimeReady.Api.Models;

namespace TimeReady.Api.Data.Repositories;

/// <summary>
/// Data access for employees. It exists so the controllers depend on an
/// intention ("give me the employees") instead of on EF Core query syntax, and
/// so the ordering used by every screen is defined in exactly one place.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>All employees, upcoming vacations first.</summary>
    Task<IReadOnlyList<Employee>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Finds one employee, or null when the id is unknown.</summary>
    /// <param name="id">Employee id.</param>
    /// <param name="tracked">True when the entity will be modified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Employee?> FindAsync(int id, bool tracked, CancellationToken cancellationToken);

    /// <summary>Stages a new employee for insertion.</summary>
    void Add(Employee employee);

    /// <summary>Stages an employee for deletion.</summary>
    void Remove(Employee employee);

    /// <summary>Commits the staged changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
