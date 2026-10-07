using System.Linq.Expressions;
using Competency.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Competency.OrgStructure;

/// <summary>
/// Answers <see cref="IEmployeeDirectory"/> lookups straight from the employee table.
/// </summary>
internal sealed class EmployeeDirectory(AppDbContext context) : IEmployeeDirectory
{
    private static readonly Expression<Func<Employee, EmployeeStatus>> StatusProjection = employee => new EmployeeStatus(
        employee.Id, employee.FullName, employee.LastName, employee.FirstName, employee.MiddleName, employee.IsActive);

    public Task<EmployeeStatus?> FindAsync(Guid employeeId, CancellationToken cancellationToken = default) => context
        .Set<Employee>()
        .AsNoTracking()
        .Where(employee => employee.Id == employeeId)
        .Select(StatusProjection)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, EmployeeStatus>> FindManyAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToArray();
        return await context
            .Set<Employee>()
            .AsNoTracking()
            .Where(employee => ids.Contains(employee.Id))
            .Select(StatusProjection)
            .ToDictionaryAsync(status => status.Id, cancellationToken);
    }

    public Task<IDbContextTransaction> BeginExclusiveAsync(CancellationToken cancellationToken = default) =>
        context.BeginExclusiveAsync(cancellationToken);
}
