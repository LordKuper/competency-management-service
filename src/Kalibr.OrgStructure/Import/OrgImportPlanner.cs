using System.Security.Cryptography;
using System.Text.Json;

namespace Kalibr.OrgStructure.Import;

/// <summary>
/// The outcome of planning: the plan, or the errors that make the file unusable against the current data.
/// </summary>
/// <param name="Plan">The plan; null when there are errors.</param>
/// <param name="Errors">The problems found; empty when there is a plan.</param>
internal sealed record PlanOutcome(ImportPlan? Plan, IReadOnlyList<ImportError> Errors);

/// <summary>
/// Compares the rows of a file with the organization as it is: matches them by name, decides each employee's main and secondary assignments,
/// and lists what the file does not name. One code for the preview and the apply, so that what is shown is what is applied.
/// </summary>
internal static class OrgImportPlanner
{
    /// <summary>
    /// Plans the import.
    /// </summary>
    /// <param name="rows">The recognised rows of the file.</param>
    /// <param name="data">The organization as it is now.</param>
    /// <param name="accounts">Says which missing employees are the last active administrator.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The plan, or the errors.</returns>
    public static async Task<PlanOutcome> PlanAsync(
        IReadOnlyList<ImportRow> rows,
        ImportSnapshot data,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        var errors = new List<ImportError>(NamesakeErrors(data));
        var resolver = new ImportResolver(data, errors);
        var people = new Dictionary<string, FilePerson>();
        foreach (var row in rows)
        {
            var unit = resolver.Unit(row);
            unit.IsDeclared = true;
            var position = row.Position is null ? null : resolver.Position(row.Position, row);
            if (position is not null)
            {
                unit.Positions.Add(position);
            }

            if (row.Person is { } person)
            {
                var key = person.Key;
                if (!people.TryGetValue(key, out var filePerson))
                {
                    people[key] = filePerson = new FilePerson(person);
                }

                filePerson.Add(new Placement(unit, position!), row.LegalEntity is null ? null : resolver.LegalEntity(row.LegalEntity, row));
            }
        }

        if (errors.Count > 0)
        {
            return new PlanOutcome(null, errors);
        }

        var employeesByKey = data.Employees.ToDictionary(employee => employee.Name.Key);
        var plan = new ImportPlan
        {
            Snapshot = data,
            Units = resolver.Units,
            Positions = resolver.Positions,
            LegalEntities = resolver.LegalEntities,
            Employees = [.. people.Values.Select(person => ChangeOf(person, employeesByKey.GetValueOrDefault(person.Name.Key), data))],
            UnitLists = [.. resolver.Units.Where(unit => unit.IsDeclared).Select(unit => ListChangeOf(unit, data)).OfType<UnitListChange>()],
            MissingUnits = MissingUnits(resolver.Units, data),
            MissingEmployees = await MissingEmployeesAsync(people.Keys.ToHashSet(), data, accounts, cancellationToken),
        };
        return new PlanOutcome(plan, []);
    }

    /// <summary>
    /// The fingerprint of a plan for a file: a digest of the file's bytes and of the plan in a fixed serialization, equal exactly when
    /// the same file gives the same plan.
    /// </summary>
    /// <param name="content">The uploaded file.</param>
    /// <param name="plan">The plan as shown, without a fingerprint.</param>
    /// <returns>The digest in lower-case hexadecimal.</returns>
    public static string Fingerprint(byte[] content, OrgImportPlanResponse plan)
    {
        byte[] material = [.. SHA256.HashData(content), .. JsonSerializer.SerializeToUtf8Bytes(plan with { Fingerprint = string.Empty })];
        return Convert.ToHexStringLower(SHA256.HashData(material));
    }

    private static IEnumerable<ImportError> NamesakeErrors(ImportSnapshot data) => data.Employees
        .GroupBy(employee => employee.Name.Key)
        .Where(group => group.Count() > 1)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => new ImportError(
            null,
            OrgImportParser.FullNameColumn,
            $"В системе несколько сотрудников с ФИО «{group.First().FullName}» ({group.Count()}): переименуйте одного из них."));

    private static EmployeeChange ChangeOf(FilePerson person, SnapshotEmployee? existing, ImportSnapshot data)
    {
        var wanted = person.Placements;
        var main = existing is null
            ? wanted[0]
            : wanted.FirstOrDefault(placement => placement.Is(existing.OrgUnitId, existing.PositionId)) ?? wanted[0];
        var secondary = wanted.Where(placement => placement != main).ToList();
        if (existing is null)
        {
            return new EmployeeChange { Name = person.Name, Main = main, Secondary = secondary, LegalEntities = person.LegalEntities, AddedAssignments = secondary };
        }

        var assignments = data.AssignmentsByEmployee[existing.Id].ToList();
        var kept = secondary
            .Select(placement => (Placement: placement, Assignment: assignments.FirstOrDefault(assignment => placement.Is(assignment.OrgUnitId, assignment.PositionId))))
            .ToList();
        return new EmployeeChange
        {
            Name = person.Name,
            Existing = existing,
            Main = main,
            Secondary = secondary,
            LegalEntities = person.LegalEntities,
            IsRehire = !existing.IsActive,
            IsTransfer = !main.Is(existing.OrgUnitId, existing.PositionId),
            AddedAssignments = [.. kept.Where(pair => pair.Assignment is null).Select(pair => pair.Placement)],
            RemovedAssignments = [.. assignments.Where(assignment => !secondary.Any(placement => placement.Is(assignment.OrgUnitId, assignment.PositionId)))],
            MainLegalEntitiesChanged = !SameEntities(person.LegalEntities[main], data.MainLegalEntityIds[existing.Id]),
            ChangedAssignmentLegalEntities =
            [
                .. kept.Where(pair => pair.Assignment is not null
                    && !SameEntities(person.LegalEntities[pair.Placement], data.AssignmentLegalEntityIds[pair.Assignment.Id]))
                    .Select(pair => pair.Placement),
            ],
        };
    }

    private static bool SameEntities(IReadOnlyList<LegalEntityRef> wanted, IEnumerable<Guid> current) =>
        wanted.All(entity => entity.Id is not null) && wanted.Select(entity => entity.Id!.Value).ToHashSet().SetEquals(current);

    private static UnitListChange? ListChangeOf(UnitNode unit, ImportSnapshot data)
    {
        var current = unit.IsNew ? [] : data.PositionIdsByUnit[unit.Id!.Value].ToHashSet();
        var wantedIds = unit.Positions.Where(position => position.Id is not null).Select(position => position.Id!.Value).ToHashSet();
        var added = unit.Positions.Where(position => position.Id is not { } id || !current.Contains(id)).ToList();
        var removed = current.Where(id => !wantedIds.Contains(id)).ToList();
        return added.Count + removed.Count > 0 ? new UnitListChange(unit, added, removed) : null;
    }

    private static List<MissingUnitResponse> MissingUnits(IReadOnlyList<UnitNode> nodes, ImportSnapshot data)
    {
        var named = nodes.Where(node => node.Id is not null).Select(node => node.Id!.Value).ToHashSet();
        return
        [
            .. data.Units
                .Where(unit => unit.IsActive && !named.Contains(unit.Id))
                .Select(unit => new MissingUnitResponse(unit.Id, data.UnitPaths[unit.Id], unit.ParentId))
                .OrderBy(unit => unit.Path, StringComparer.Ordinal)
                .ThenBy(unit => unit.Id),
        ];
    }

    private static async Task<List<MissingEmployeeResponse>> MissingEmployeesAsync(
        HashSet<string> namedKeys,
        ImportSnapshot data,
        IEmployeeAccounts accounts,
        CancellationToken cancellationToken)
    {
        var missing = new List<MissingEmployeeResponse>();
        foreach (var employee in data.Employees.Where(employee => employee.IsActive && !namedKeys.Contains(employee.Name.Key)))
        {
            var isLast = data.EmployeesWithAccount.Contains(employee.Id) && await accounts.IsLastActiveAdministratorAsync(employee.Id, cancellationToken);
            var unitIds = data.AssignmentsByEmployee[employee.Id].Select(assignment => assignment.OrgUnitId).Prepend(employee.OrgUnitId).Distinct().Order();
            missing.Add(new MissingEmployeeResponse(employee.Id, employee.FullName, [.. unitIds], isLast));
        }

        return [.. missing.OrderBy(employee => employee.FullName, StringComparer.Ordinal).ThenBy(employee => employee.Id)];
    }

    private sealed class FilePerson(PersonName name)
    {
        private readonly Dictionary<Placement, List<LegalEntityRef>> legalEntities = [];

        public PersonName Name { get; } = name;

        public List<Placement> Placements { get; } = [];

        public IReadOnlyDictionary<Placement, List<LegalEntityRef>> LegalEntities => legalEntities;

        public void Add(Placement placement, LegalEntityRef? entity)
        {
            if (!legalEntities.TryGetValue(placement, out var entities))
            {
                legalEntities[placement] = entities = [];
                Placements.Add(placement);
            }

            if (entity is not null && !entities.Contains(entity))
            {
                entities.Add(entity);
            }
        }
    }
}
