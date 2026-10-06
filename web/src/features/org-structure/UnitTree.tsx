import { Grid } from "antd";
import type { OrgUnit } from "./orgStructureApi";
import type { UnitNode, UnitSearch } from "./orgTree";
import { type UnitAction, UnitCard, type UnitOpenness } from "./UnitCard";
import { UnitEmployees } from "./UnitEmployees";

/** What every card of the hierarchy needs to know about the screen it is on. */
export interface UnitTreeView {
  openIds: ReadonlySet<string>;
  /** The name search in progress; it narrows the units shown, and null means every unit is shown. */
  search: UnitSearch | null;
  selectedId: string | null;
  isAdmin: boolean;
  onToggle: (unitId: string) => void;
  onAction: (action: UnitAction, unit: OrgUnit) => void;
  onOpenEmployee: (employeeId: string) => void;
}

function visibleNodes(nodes: readonly UnitNode[], view: UnitTreeView) {
  const visibleIds = view.search?.visibleIds;
  return visibleIds
    ? nodes.filter(({ unit }) => visibleIds.has(unit.id))
    : nodes;
}

function opennessOf(unit: OrgUnit, view: UnitTreeView): UnitOpenness {
  if (!view.openIds.has(unit.id)) return "closed";
  const { search } = view;
  const isOnlyOnPath =
    search?.pathIds.has(unit.id) &&
    !unit.name.toLocaleLowerCase("ru").includes(search.needle);
  return isOnlyOnPath ? "path" : "open";
}

function UnitNodeView({ node, view }: { node: UnitNode; view: UnitTreeView }) {
  const { unit } = node;
  const openness = opennessOf(unit, view);
  const children = visibleNodes(node.children, view);

  return (
    <li className="org-tree__node">
      <UnitCard
        unit={unit}
        childCount={children.length}
        openness={openness}
        isSelected={view.selectedId === unit.id}
        needle={view.search?.needle ?? ""}
        isAdmin={view.isAdmin}
        onToggle={view.onToggle}
        onAction={view.onAction}
        onOpenEmployee={view.onOpenEmployee}
      />
      {openness !== "closed" && (
        <ul className="org-tree__children">
          {children.map((child) => (
            <UnitNodeView key={child.unit.id} node={child} view={view} />
          ))}
          {openness === "open" && (
            <UnitEmployees
              unitId={unit.id}
              hasChildUnits={children.length > 0}
              isAdmin={view.isAdmin}
              onOpenEmployee={view.onOpenEmployee}
            />
          )}
        </ul>
      )}
    </li>
  );
}

/**
 * The hierarchy of unit and employee cards: a unit opens to its child units and then its own employees, each level
 * indented under its parent with connecting lines. The indent shrinks below the desktop width.
 */
export function UnitTree({
  roots,
  view,
}: {
  roots: readonly UnitNode[];
  view: UnitTreeView;
}) {
  const screens = Grid.useBreakpoint();
  return (
    <ul
      className={screens.lg ? "org-tree" : "org-tree org-tree--narrow"}
      aria-label="Иерархия подразделений"
    >
      {visibleNodes(roots, view).map((root) => (
        <UnitNodeView key={root.unit.id} node={root} view={view} />
      ))}
    </ul>
  );
}
