import type { OrgUnit, OrgUnitTreeNode } from "./orgStructureApi";

/** A unit together with the units directly below it. */
export interface UnitNode {
  unit: OrgUnitTreeNode;
  children: UnitNode[];
}

/** What a name search shows: the matching units, the units above them and everything below a match. */
export interface UnitSearch {
  /** The search text, trimmed and lower-cased. */
  needle: string;
  visibleIds: ReadonlySet<string>;
  /** Units above a match; opened so that every match is in view. */
  pathIds: ReadonlySet<string>;
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

/** Finds the units whose name contains the text, ignoring letter case. */
export function searchUnitTree(
  roots: readonly UnitNode[],
  text: string,
): UnitSearch {
  const needle = text.trim().toLocaleLowerCase("ru");
  const visibleIds = new Set<string>();
  const pathIds = new Set<string>();

  function visit({ unit, children }: UnitNode, isBelowMatch: boolean): boolean {
    const isMatch = unit.name.toLocaleLowerCase("ru").includes(needle);
    let hasMatchBelow = false;
    for (const child of children) {
      hasMatchBelow = visit(child, isBelowMatch || isMatch) || hasMatchBelow;
    }
    if (isBelowMatch || isMatch || hasMatchBelow) visibleIds.add(unit.id);
    if (hasMatchBelow) pathIds.add(unit.id);
    return isMatch || hasMatchBelow;
  }

  for (const root of roots) visit(root, false);
  return { needle, visibleIds, pathIds };
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
