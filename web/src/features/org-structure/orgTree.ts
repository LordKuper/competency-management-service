import type { OrgUnit } from "./orgStructureApi";

/** A unit together with the units directly below it. */
export interface UnitNode {
  unit: OrgUnit;
  children: UnitNode[];
}

/** Units found by a name search, with the ancestors that must be open to show them. */
export interface UnitSearchResult {
  visible: OrgUnit[];
  expandedIds: string[];
}

/** Arranges units by their parent links, siblings in the given order; a unit whose parent is absent from the list becomes a root. */
export function buildUnitTree(units: readonly OrgUnit[]): UnitNode[] {
  const nodes = new Map<string, UnitNode>(
    units.map((unit) => [unit.id, { unit, children: [] }]),
  );
  const roots: UnitNode[] = [];
  for (const node of nodes.values()) {
    const parent =
      node.unit.parentId === null ? undefined : nodes.get(node.unit.parentId);
    (parent?.children ?? roots).push(node);
  }
  return roots;
}

/** Ids of the units above the given one, nearest first; empty for a root or an unknown unit. */
export function ancestorIds(units: readonly OrgUnit[], id: string): string[] {
  const byId = indexById(units);
  const unit = byId.get(id);
  return unit ? ancestorsIn(byId, unit) : [];
}

/** The units whose name contains the text, ignoring letter case, together with their ancestors. */
export function searchUnits(
  units: readonly OrgUnit[],
  text: string,
): UnitSearchResult {
  const needle = text.trim().toLocaleLowerCase("ru");
  const byId = indexById(units);
  const shown = new Set<string>();
  const expanded = new Set<string>();
  for (const unit of units) {
    if (!unit.name.toLocaleLowerCase("ru").includes(needle)) continue;
    shown.add(unit.id);
    for (const id of ancestorsIn(byId, unit)) {
      shown.add(id);
      expanded.add(id);
    }
  }
  return {
    visible: units.filter((unit) => shown.has(unit.id)),
    expandedIds: [...expanded],
  };
}

function indexById(units: readonly OrgUnit[]): Map<string, OrgUnit> {
  return new Map(units.map((unit) => [unit.id, unit]));
}

function ancestorsIn(byId: ReadonlyMap<string, OrgUnit>, unit: OrgUnit) {
  const ids: string[] = [];
  let parentId = unit.parentId;
  while (parentId !== null && !ids.includes(parentId)) {
    const parent = byId.get(parentId);
    if (!parent) break;
    ids.push(parentId);
    parentId = parent.parentId;
  }
  return ids;
}
