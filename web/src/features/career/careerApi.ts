import { queryOptions, useQuery } from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import { useIsAdmin } from "../auth/useCurrentUser";

/** The HR assigned to a unit or inherited from the units above, each null when absent. */
export type OrgUnitHr = components["schemas"]["OrgUnitHrResponse"];

/** The person behind an assignment, with the unit that holds it. */
export type OrgUnitHrPerson = components["schemas"]["OrgUnitHrPersonResponse"];

/** Units the signed-in user heads or is the responsible HR of. */
export type ProcessRoles = components["schemas"]["ProcessRolesResponse"];

/** Cache key prefix of every career query. */
export const careerQueryKey = ["career"] as const;

/** Assigned and inherited HR of a unit. */
export function unitHrQuery(orgUnitId: string) {
  return queryOptions({
    queryKey: [...careerQueryKey, "unit-hr", orgUnitId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/org-units/{id}/hr", {
          params: { path: { id: orgUnitId } },
        }),
      ),
    staleTime: 60_000,
    refetchOnWindowFocus: false,
  });
}

/**
 * The HR who answers for the unit: its own assignment, or the inherited one when there is none. An inactive assignee is
 * not replaced by the inherited HR, because the server does not route to one either. Only administrators get `isActive`;
 * for everyone else the server leaves out the assignments whose account cannot act, so what it returns can act.
 */
export function effectiveHr(hr: OrgUnitHr): OrgUnitHrPerson | null {
  const person = hr.assigned ?? hr.inherited;
  return person && person.isActive !== false ? person : null;
}

/** Process roles of the signed-in user; the section shows what these units allow. */
export function useProcessRoles() {
  return useQuery({
    queryKey: [...careerQueryKey, "me"],
    queryFn: async () => unwrap(await api.GET("/api/v1/career/me")),
  });
}

/** Whether the "На согласовании" tab is shown: heads and HR of some unit, and administrators. */
export function useCanApprove(): boolean {
  const isAdmin = useIsAdmin();
  const { data } = useProcessRoles();
  return (
    isAdmin ||
    (data ? data.headedUnits.length + data.hrUnits.length > 0 : false)
  );
}
