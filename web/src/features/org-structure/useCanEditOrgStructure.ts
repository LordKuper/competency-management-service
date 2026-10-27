import { useIsAdmin, useIsHr } from "../auth/useCurrentUser";

/** Whether the signed-in account changes units, employees and the lists of positions of units: global administrators and HR. */
export function useCanEditOrgStructure(): boolean {
  const isAdmin = useIsAdmin();
  const isHr = useIsHr();
  return isAdmin || isHr;
}
