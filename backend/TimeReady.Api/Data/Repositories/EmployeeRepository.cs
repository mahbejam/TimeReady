using Microsoft.EntityFrameworkCore;
using TimeReady.Api.Models;

namespace TimeReady.Api.Data.Repositories;

/// <inheritdoc cref="IEmployeeRepository" />
public sealed class EmployeeRepository(AppDbContext context) : IEmployeeRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Employee>> ListAsync(CancellationToken cancellationToken) =>
        await context.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.VacationStartDate == null)
            .ThenBy(employee => employee.VacationStartDate)
            .ThenBy(employee => employee.FullName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Employee?> FindAsync(int id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? context.Employees : context.Employees.AsNoTracking();

        return query.FirstOrDefaultAsync(employee => employee.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Employee employee) => context.Employees.Add(employee);

    /// <inheritdoc />
    public void Remove(Employee employee) => context.Employees.Remove(employee);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
