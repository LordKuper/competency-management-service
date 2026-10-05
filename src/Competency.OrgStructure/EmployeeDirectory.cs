using Competency.Platform;
using Microsoft.EntityFrameworkCore;

namespace Competency.OrgStructure;

/// <summary>
/// Answers <see cref="IEmployeeDirectory"/> lookups straight from the employee table.
/// </summary>
internal sealed class EmployeeDirectory(AppDbContext context) : IEmployeeDirectory
{
    public Task<EmployeeStatus?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default) => context
        .Set<Employee>()
        .AsNoTracking()
        .Where(employee => employee.Id == employeeId)
        .Select(employee => new EmployeeStatus(employee.Id, employee.FullName, employee.IsActive))
        .FirstOrDefaultAsync(cancellationToken);
}
