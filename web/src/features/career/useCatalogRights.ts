import { useMemo } from "react";
import { useIsAdmin, useIsHr } from "../auth/useCurrentUser";
import { useProcessRoles } from "./careerApi";

/**
 * What the signed-in user may do with catalogue entries, from the account role and the process roles. It only decides
 * which actions are offered; the server decides each request again and answers 403 to one that is not allowed. Whether an
 * entry may be edited or deleted the server tells with the entry itself.
 * Directions, competencies and scales belong to no unit: administrators and accounts with the HR role keep them and heads of
 * units add drafts. A track and its levels belong to the track's unit: its HR and administrators keep them, its head edits drafts.
 * Adding a track needs a unit to put it in. `canManage` takes the unit of the track an entry is or belongs to, none for an
 * entry without a unit.
 */
export function useCatalogRights() {
  const isAdmin = useIsAdmin();
  const hasHrRole = useIsHr();
  const { data } = useProcessRoles();

  return useMemo(() => {
    const headed = new Set(data?.headedUnits.map((unit) => unit.id));
    const hr = new Set(data?.hrUnits.map((unit) => unit.id));
    const isHr = isAdmin || hasHrRole;
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
      canCreate: isHr || headed.size > 0,
      canCreateTrack: isAdmin || ownerUnits.length > 0,
      canManage: (unitId?: string) =>
        unitId === undefined ? isHr : keeps(unitId),
    };
  }, [isAdmin, hasHrRole, data]);
}
