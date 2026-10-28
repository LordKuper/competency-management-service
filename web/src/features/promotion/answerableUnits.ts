import { useQuery } from "@tanstack/react-query";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "../career/careerApi";
import {
  type OrgUnitTreeNode,
  orgUnitTreeQuery,
} from "../org-structure/orgStructureApi";
import { buildUnitTree, type UnitNode } from "../org-structure/orgTree";

/** A unit as antd `Tree` and `TreeSelect` take it. */
export interface UnitTreeItem {
  key: string;
  value: string;
  title: string;
  children: UnitTreeItem[];
}

/** The units arranged by their parent links; a unit whose parent is not among them becomes a root. */
export function unitTreeItems(
  units: readonly OrgUnitTreeNode[],
): UnitTreeItem[] {
  const toItems = (nodes: readonly UnitNode[]): UnitTreeItem[] =>
    nodes.map(({ unit, children }) => ({
      key: unit.id,
      value: unit.id,
      title: unit.name,
      children: toItems(children),
    }));
  return toItems(buildUnitTree(units));
}

/**
 * The working units the signed-in user may choose for a wave or a commission: all of them for an administrator, the
 * ones the user answers for as HR otherwise. `alsoShown` keeps units already chosen visible even when they are no longer
 * the user's to choose.
 */
export function useAnswerableUnits(alsoShown: readonly string[] = []) {
  const isAdmin = useIsAdmin();
  const tree = useQuery(orgUnitTreeQuery);
  const roles = useProcessRoles();
  const answerable = new Set([
    ...(roles.data?.hrUnits.map((unit) => unit.id) ?? []),
    ...alsoShown,
  ]);
  const isReady =
    tree.data !== undefined && (isAdmin || roles.data !== undefined);
  return {
    units: isReady
      ? tree.data?.filter((unit) =>
          isAdmin
            ? unit.isActive || answerable.has(unit.id)
            : answerable.has(unit.id),
        )
      : undefined,
    error: tree.error ?? (isAdmin ? null : roles.error),
    refetch: () => {
      void tree.refetch();
      void roles.refetch();
    },
  };
}
