import type { Employee, OrgUnit, OrgUnitTreeNode } from "./orgStructureApi";

/** A unit together with the units directly below it. */
export interface UnitNode {
  unit: OrgUnitTreeNode;
  children: UnitNode[];
}

/** What a search shows: the matching units and the units of the matched employees, the units above them and everything below a unit match. */
export interface UnitSearch {
  /** The search text, trimmed and lower-cased. */
  needle: string;
  visibleIds: ReadonlySet<string>;
  /** Units above a match or holding matched employees; opened so that every match is in view. */
  pathIds: ReadonlySet<string>;
  /** The matched employees by the unit they work in. */
  employeesByUnit: ReadonlyMap<string, readonly Employee[]>;
}

/** Arranges units by their parent links, siblings in the given order; a unit whose parent is absent from the list becomes a root. */
export function buildUnitTree(units: readonly OrgUnitTreeNode[]): UnitNode[] {
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

/** Finds the units whose name contains the text, ignoring letter case, together with the units of the employees already matched to it. */
export function searchUnitTree(
  roots: readonly UnitNode[],
  text: string,
  matchedEmployees: readonly Employee[],
): UnitSearch {
  const needle = text.trim().toLocaleLowerCase("ru");
  const visibleIds = new Set<string>();
  const pathIds = new Set<string>();
  const employeesByUnit = new Map<string, Employee[]>();
  for (const employee of matchedEmployees) {
    const inUnit = employeesByUnit.get(employee.orgUnitId);
    if (inUnit) inUnit.push(employee);
    else employeesByUnit.set(employee.orgUnitId, [employee]);
  }

  function visit({ unit, children }: UnitNode, isBelowMatch: boolean): boolean {
    const isMatch = unit.name.toLocaleLowerCase("ru").includes(needle);
    const hasMatchedEmployees = employeesByUnit.has(unit.id);
    let hasMatchBelow = false;
    for (const child of children) {
      hasMatchBelow = visit(child, isBelowMatch || isMatch) || hasMatchBelow;
    }
    const isOnPath = hasMatchBelow || hasMatchedEmployees;
    if (isBelowMatch || isMatch || isOnPath) visibleIds.add(unit.id);
    if (isOnPath) pathIds.add(unit.id);
    return isMatch || isOnPath;
  }

  for (const root of roots) visit(root, false);
  return { needle, visibleIds, pathIds, employeesByUnit };
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
