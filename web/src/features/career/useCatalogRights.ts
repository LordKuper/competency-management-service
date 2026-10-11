import { useMemo } from "react";
import { useIsAdmin } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";
import type { CatalogStatus } from "./catalogApi";

interface Rated {
  status: CatalogStatus;
}

/**
 * What the signed-in user may do with catalogue entries, from the account role and the process roles. It only decides
 * which actions are offered; the server decides each request again and answers 403 to one that is not allowed.
 * Directions, categories, competencies and scales belong to no unit: administrators and any HR keep them and heads of
 * units add drafts. A track and its levels belong to the track's unit: its HR and administrators keep them, its head edits drafts.
 */
export function useCatalogRights() {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();

  return useMemo(() => {
    const headed = new Set(data?.headedUnits.map((unit) => unit.id));
    const hr = new Set(data?.hrUnits.map((unit) => unit.id));
    const isHr = isAdmin || hr.size > 0;
    const ownerUnits = [
      ...(data?.headedUnits ?? []),
      ...(data?.hrUnits ?? []),
    ].filter(
      (unit, index, units) =>
        units.findIndex((u) => u.id === unit.id) === index,
    );
    const keeps = (unitId: string) => isAdmin || hr.has(unitId);
    return {
      isAdmin,
      isHr,
      ownerUnits,
      /** Whether the user may add a direction, category, competency or scale. */
      canCreate: isHr || headed.size > 0,
      /** Whether the user may add a track: they need a unit to put it in. */
      canCreateTrack: isAdmin || ownerUnits.length > 0,
      /** `unitId` is the unit of the track the entry is, or belongs to; omitted for an entry without a unit. */
      canEdit: (entry: Rated, unitId?: string) =>
        unitId === undefined
          ? isHr || (entry.status === "Draft" && headed.size > 0)
          : keeps(unitId) || (entry.status === "Draft" && headed.has(unitId)),
      canManage: (unitId?: string) =>
        unitId === undefined ? isHr : keeps(unitId),
    };
  }, [isAdmin, data]);
}

export type CatalogRights = ReturnType<typeof useCatalogRights>;
