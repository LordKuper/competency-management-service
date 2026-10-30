namespace Kalibr.OrgStructure.Import;

/// <summary>
/// Matches the names of the file with the units, positions and legal entities of the system, creating one node per distinct name or path
/// and reporting every name the system holds more than once, once at its first use. The comparison key ignores letter case, «ё» versus «е» and spacing.
/// A unit is matched by its path from the root, so units of one name under different parents stay apart.
/// </summary>
internal sealed class ImportResolver
{
    private const char PathKeySeparator = '\u001F';

    private readonly List<ImportError> errors;
    private readonly ILookup<(Guid? ParentId, string Key), SnapshotUnit> systemUnits;
    private readonly ILookup<string, SnapshotPosition> systemPositions;
    private readonly ILookup<string, SnapshotLegalEntity> systemLegalEntities;
    private readonly Dictionary<string, UnitNode> units = [];
    private readonly Dictionary<string, PositionRef> positions = [];
    private readonly Dictionary<string, LegalEntityRef> legalEntities = [];

    public ImportResolver(ImportSnapshot data, List<ImportError> errors)
    {
        this.errors = errors;
        systemUnits = data.Units.ToLookup(unit => (unit.ParentId, ImportText.Key(ImportText.Clean(unit.Name))));
        systemPositions = data.Positions.ToLookup(position => ImportText.Key(ImportText.Clean(position.Name)));
        systemLegalEntities = data.LegalEntities.ToLookup(entity => ImportText.Key(ImportText.Clean(entity.Name)));
    }

    /// <summary>
    /// Every unit of the file's tree in the order met, each after its parent.
    /// </summary>
    public List<UnitNode> Units { get; } = [];

    public List<PositionRef> Positions { get; } = [];

    public List<LegalEntityRef> LegalEntities { get; } = [];

    /// <summary>
    /// Finds or creates the nodes of the row's path and returns the last one, the unit the row is about.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The unit node.</returns>
    public UnitNode Unit(ImportRow row)
    {
        UnitNode? parent = null;
        foreach (var step in row.Path)
        {
            var key = parent is null ? ImportText.Key(step.Name) : parent.PathKey + PathKeySeparator + ImportText.Key(step.Name);
            if (!units.TryGetValue(key, out var node))
            {
                node = CreateUnit(step, key, parent, row.Number);
                units[key] = node;
                Units.Add(node);
            }

            parent = node;
        }

        return parent!;
    }

    public PositionRef Position(string name, ImportRow row)
    {
        var key = ImportText.Key(name);
        if (positions.TryGetValue(key, out var known))
        {
            return known;
        }

        var matches = systemPositions[key].ToList();
        if (matches.Count > 1)
        {
            errors.Add(new ImportError(row.Number, OrgImportParser.PositionColumn, $"В справочнике несколько должностей «{name}», различающихся только регистром или буквой «ё»."));
        }

        var match = matches.FirstOrDefault();
        var created = match is null
            ? new PositionRef { Name = name, IsNew = true }
            : new PositionRef { Name = match.Name, Id = match.Id, WasInactive = !match.IsActive };
        positions[key] = created;
        Positions.Add(created);
        return created;
    }

    public LegalEntityRef LegalEntity(string name, ImportRow row)
    {
        var key = ImportText.Key(name);
        if (legalEntities.TryGetValue(key, out var known))
        {
            return known;
        }

        var matches = systemLegalEntities[key].ToList();
        if (matches.Count > 1)
        {
            errors.Add(new ImportError(row.Number, OrgImportParser.LegalEntityColumn, $"В справочнике несколько юридических лиц «{name}», различающихся только регистром или буквой «ё»."));
        }

        var match = matches.FirstOrDefault();
        var created = match is null
            ? new LegalEntityRef { Name = name, IsNew = true }
            : new LegalEntityRef { Name = match.Name, Id = match.Id };
        legalEntities[key] = created;
        LegalEntities.Add(created);
        return created;
    }

    private UnitNode CreateUnit(UnitStep step, string key, UnitNode? parent, int rowNumber)
    {
        var matches = parent is { IsNew: true }
            ? []
            : systemUnits[(parent?.Id, ImportText.Key(step.Name))].ToList();
        if (matches.Count > 1)
        {
            var path = parent is null ? step.Name : $"{parent.PathText} / {step.Name}";
            errors.Add(new ImportError(rowNumber, OrgImportParser.UnitColumn(step.Level), $"В системе несколько подразделений с путём «{path}»: переименуйте одно из них."));
        }

        return matches.FirstOrDefault() is { } match
            ? new UnitNode { Name = match.Name, PathKey = key, Parent = parent, Id = match.Id, WasInactive = !match.IsActive }
            : new UnitNode { Name = step.Name, PathKey = key, Parent = parent, IsNew = true };
    }
}
