import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { api } from "../../api/client";
import { deskQueryKey } from "../../api/queryClient";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** The signed-in user's desk: the number of expected actions and the non-empty groups with their first items. */
export type Desk = components["schemas"]["DeskResponse"];
/** One group of the desk with its full count. */
export type DeskGroupView = components["schemas"]["DeskGroupResponse"];
/** Kind of a desk group; the client words its title. */
export type DeskGroup = components["schemas"]["DeskGroup"];
/** One object on the desk with every reason it is there. */
export type DeskItem = components["schemas"]["DeskItemResponse"];
/** One reason of a desk item; the client words it from its kind. */
export type DeskReason = components["schemas"]["DeskReasonResponse"];
/** Kind of a desk reason. */
export type DeskKind = components["schemas"]["DeskKind"];
/** A record a desk item is about or names. */
export type DeskObject = components["schemas"]["DeskObject"];

/** A fact about the system that a scenario card needs before it is shown. */
export type DeskPrerequisite = components["schemas"]["DeskPrerequisite"];

/** How many items of a group the desk response carries. */
export const FIRST_ITEMS = 10;

/** How many more items each "Показать ещё" adds. */
export const MORE_ITEMS = 20;

/** Cache key of the number of the user's expected actions, reread on its own when the address changes. */
export const deskCountQueryKey = [...deskQueryKey, "count"] as const;

/** The desk; it stays fresh-by-default so it is reread on focus and after any successful change. */
export function useDesk() {
  return useQuery({
    queryKey: [...deskQueryKey, "board"],
    queryFn: async () => unwrap(await api.GET("/api/v1/desk")),
  });
}

/** The number of the user's expected actions, shown on the desk menu entry. */
export function useDeskCount() {
  return useQuery({
    queryKey: deskCountQueryKey,
    queryFn: async () => unwrap(await api.GET("/api/v1/desk/count")),
  });
}

/**
 * The first `limit` items of one group, read again from the start because items leave as they are done; only asked
 * for beyond what the desk response already carries. The rows shown stay while the next read runs.
 */
export function useDeskGroupItems(group: DeskGroup, limit: number) {
  return useQuery({
    queryKey: [...deskQueryKey, "items", group, limit],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/desk/items", {
          params: { query: { group: [group], limit } },
        }),
      ),
    enabled: limit > FIRST_ITEMS,
    placeholderData: keepPreviousData,
  });
}

/** What a unit lacks to work, row by row. */
export type UnitSetup = components["schemas"]["UnitSetupResponse"];
/** The signed-in employee's place in the company. */
export type MyPlace = components["schemas"]["MyPlaceResponse"];

/**
 * What a unit lacks to work. It lives under the desk key, so it is reread after any successful change, which is how
 * every action on the screen recounts the rows.
 */
export function unitSetupQuery(unitId: string) {
  return queryOptions({
    queryKey: [...deskQueryKey, "unit-setup", unitId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/career/org-units/{id}/setup", {
          params: { path: { id: unitId } },
        }),
      ),
  });
}

/** The signed-in employee's unit, position, head, HR, tracks and requirements of the levels above. */
export const myPlaceQuery = queryOptions({
  queryKey: ["career", "my-place"],
  queryFn: async () => unwrap(await api.GET("/api/v1/career/me/place")),
});

/** The groups of findings the organisation structure queue works through. */
export const ORG_QUEUE_GROUPS = [
  "OrgCleanup",
  "UnitsToConnect",
  "InactivePositions",
  "UnusedPositions",
  "EmptyUnits",
] as const satisfies readonly DeskGroup[];

/** The first `limit` findings of one group, or of all the groups of the queue; items leave as they are done, so it is read from the start. */
export function orgQueueQuery(limit: number, group?: DeskGroup) {
  return queryOptions({
    queryKey: [...deskQueryKey, "org-queue", group ?? "all", limit],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/desk/items", {
          params: {
            query: { group: group ? [group] : [...ORG_QUEUE_GROUPS], limit },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  });
}
