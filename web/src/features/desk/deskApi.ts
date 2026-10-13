import { keepPreviousData, useQuery } from "@tanstack/react-query";
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
