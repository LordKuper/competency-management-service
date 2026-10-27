namespace Competency.OrgStructure.Import;

/// <summary>
/// A head chosen for a unit of the plan, checked against the plan.
/// </summary>
/// <param name="Unit">The unit.</param>
/// <param name="EmployeeKey">The key of the chosen employee; null for a unit left without a head.</param>
internal sealed record HeadChoice(UnitNode Unit, string? EmployeeKey);

/// <summary>
/// Recognises the employees of a file who may head their unit by the name of their position, offers the head step of the preview, and checks the user's choices.
/// A unit is headed by an employee with any assignment in it, so candidates and choices are read over every placement of the plan.
/// </summary>
internal static class ImportHeads
{
    /// <summary>
    /// The words and phrases a head's position name starts with, in the comparison form of <see cref="ImportText.Key"/>.
    /// </summary>
    private static readonly string[] HeadPositionStarts =
    [
        "руководитель",
        "директор",
        "генеральный директор",
        "вице-президент",
        "президент",
        "начальник",
        "заведующий",
        "управляющий",
        "глава",
    ];

    /// <summary>
    /// Whether a position name starts with a head word or phrase, ending at a word boundary: "Директор по персоналу" does, "Заместитель директора" and "Директорат" do not.
    /// </summary>
    /// <param name="positionName">The position name.</param>
    /// <returns><see langword="true"/> when an employee in the position is a candidate for the head of their unit.</returns>
    public static bool IsHeadPosition(string positionName)
    {
        var key = ImportText.Key(ImportText.Clean(positionName));
        return HeadPositionStarts.Any(start =>
            key.StartsWith(start, StringComparison.Ordinal) && (key.Length == start.Length || !char.IsLetterOrDigit(key[start.Length])));
    }

    /// <summary>
    /// Describes the head step: for every unit of the file that has candidates or a head, who may head it and who is suggested.
    /// </summary>
    /// <param name="plan">The plan, before it is applied.</param>
    /// <returns>The units in file order and the employees they refer to.</returns>
    public static ImportHeadsResponse Describe(ImportPlan plan)
    {
        var data = plan.Snapshot;
        var unitsById = data.Units.ToDictionary(unit => unit.Id);
        var employeesById = data.Employees.ToDictionary(employee => employee.Id);
        var placed = plan.Employees
            .SelectMany(change => change.Placements.Select(placement => (placement.Unit, Change: change)))
            .Distinct()
            .ToLookup(pair => pair.Unit, pair => pair.Change);
        var candidates = plan.Employees
            .SelectMany(change => change.Placements.Where(placement => IsHeadPosition(placement.Position.Name)).Select(placement => (placement.Unit, Change: change)))
            .Distinct()
            .ToLookup(pair => pair.Unit, pair => pair.Change);
        var units = new List<ImportHeadUnitResponse>();
        var employees = new List<ImportHeadEmployeeResponse>();
        var known = new HashSet<string>();

        void Name(string key, string fullName)
        {
            if (known.Add(key))
            {
                employees.Add(new ImportHeadEmployeeResponse(key, fullName));
            }
        }

        foreach (var node in plan.Units)
        {
            var head = node.Id is { } unitId && unitsById.TryGetValue(unitId, out var unit) && unit.HeadEmployeeId is { } headId ? employeesById[headId] : null;
            var found = candidates[node].ToList();
            if (found.Count == 0 && head is null)
            {
                continue;
            }

            var workers = placed[node].ToList();
            var stays = head is not null && workers.Any(change => change.Existing?.Id == head.Id);
            var suggested = stays ? head!.Id.ToString() : found.Count == 1 ? found[0].Key : null;
            foreach (var change in workers)
            {
                Name(change.Key, change.FullName);
            }

            if (head is not null)
            {
                Name(head.Id.ToString(), head.FullName);
            }

            units.Add(new ImportHeadUnitResponse(
                node.PathKey,
                node.PathText,
                node.Id,
                head?.Id.ToString(),
                stays,
                [.. found.Select(change => change.Key)],
                [.. workers.Select(change => change.Key)],
                suggested));
        }

        return new ImportHeadsResponse(units, employees);
    }

    /// <summary>
    /// Checks the user's head choices: each names a unit of the head step once, and an employee who works in the unit after the apply, which is an employee of the file
    /// with an assignment there or a missing employee of the unit whom the decisions keep.
    /// </summary>
    /// <param name="plan">The plan, before it is applied.</param>
    /// <param name="shown">The plan as the preview shows it.</param>
    /// <param name="requested">The choices as the request carries them.</param>
    /// <param name="effective">The decisions about the missing units and employees.</param>
    /// <param name="choices">The checked choices; empty when the request is invalid.</param>
    /// <returns>The reason the request is invalid, addressed to the user; <see langword="null"/> when it is valid.</returns>
    public static string? Resolve(
        ImportPlan plan,
        OrgImportPlanResponse shown,
        IReadOnlyList<ImportHeadDecisionRequest> requested,
        EffectiveDecisions effective,
        out IReadOnlyList<HeadChoice> choices)
    {
        choices = [];
        var units = shown.Heads.Units.ToDictionary(unit => unit.Key);
        var nodes = plan.Units.ToDictionary(node => node.PathKey);
        var dismissedIds = effective.EmployeesToDismiss.Select(employee => employee.Id).ToHashSet();
        var kept = shown.MissingEmployees.Where(employee => !dismissedIds.Contains(employee.Id)).ToDictionary(employee => employee.Id.ToString());
        var seen = new HashSet<string>();
        var resolved = new List<HeadChoice>();
        foreach (var request in requested)
        {
            if (!units.TryGetValue(request.Unit, out var unit))
            {
                return "Решение о руководителе относится к подразделению, которого нет на шаге «Руководители».";
            }

            if (!seen.Add(request.Unit))
            {
                return "Решение о руководителе по одному подразделению указано дважды.";
            }

            if (request.Employee is { } key && !Works(unit, key, kept))
            {
                return $"Руководителем подразделения «{unit.Path}» может быть только сотрудник, который работает в нём после применения.";
            }

            resolved.Add(new HeadChoice(nodes[request.Unit], request.Employee));
        }

        choices = resolved;
        return null;
    }

    private static bool Works(ImportHeadUnitResponse unit, string employeeKey, IReadOnlyDictionary<string, MissingEmployeeResponse> kept) =>
        unit.Employees.Contains(employeeKey)
        || (unit.UnitId is { } unitId && kept.TryGetValue(employeeKey, out var missing) && missing.OrgUnitIds.Contains(unitId));
}
