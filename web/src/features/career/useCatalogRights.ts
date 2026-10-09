import { useMemo } from "react";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";
import type { CatalogEntry } from "./catalogApi";

type Owned = Pick<CatalogEntry, "ownerOrgUnitId" | "status">;

/**
 * What the signed-in user may do with catalogue entries, from the account role and the process roles. It only decides
 * which actions are offered; the server decides each request again and answers 403 to one that is not allowed.
 * Administrators keep every entry, the HR of a unit keeps the unit's entries, and the head of a unit edits its drafts.
 */
export function useCatalogRights() {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();

  return useMemo(() => {
    const headed = new Set(data?.headedUnits.map((unit) => unit.id));
    const hr = new Set(data?.hrUnits.map((unit) => unit.id));
    const ownerUnits = [
      ...(data?.headedUnits ?? []),
      ...(data?.hrUnits ?? []),
    ].filter(
      (unit, index, units) =>
        units.findIndex((u) => u.id === unit.id) === index,
    );
    const keeps = (ownerId: string | null | undefined) =>
      isAdmin || (!!ownerId && hr.has(ownerId));
    const headsDraftOf = (ownerId: string | null | undefined) =>
      !!ownerId && headed.has(ownerId);
    return {
      isAdmin,
      isHr: isAdmin || hr.size > 0,
      ownerUnits,
      canCreate: isAdmin || ownerUnits.length > 0,
      keeps,
      canCreateFor: (ownerId: string | null | undefined) =>
        keeps(ownerId) || headsDraftOf(ownerId),
      canEdit: (entry: Owned) =>
        keeps(entry.ownerOrgUnitId) ||
        (entry.status === "Draft" && headsDraftOf(entry.ownerOrgUnitId)),
      canManage: (entry: Owned) => keeps(entry.ownerOrgUnitId),
    };
  }, [isAdmin, data]);
}

export type CatalogRights = ReturnType<typeof useCatalogRights>;
