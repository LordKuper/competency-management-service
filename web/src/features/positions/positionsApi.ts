import {
  keepPreviousData,
  type QueryClient,
  queryOptions,
} from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import { employeesQueryKey } from "../org-structure/orgStructureApi";

/** A catalogue position as the API returns it. */
export type Position = components["schemas"]["PositionResponse"];

/** A position allowed in a unit, with the unit whose list holds it: the unit itself, or a unit above it for an inherited one. */
export type UnitPosition = components["schemas"]["OrgUnitPositionResponse"];

/** Filters and paging of the catalogue list; pages count from 1. */
export interface PositionListParams {
  q?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
}

/** Cache key prefix of every catalogue query. */
const positionsQueryKey = ["positions"] as const;

/** Cache key prefix of every unit's list of positions, so one invalidation rereads the inherited lists of the units below too. */
const unitPositionsQueryKey = ["unit-positions"] as const;

/** One page of the catalogue; the previous page stays on screen while the next loads. */
export function positionListQuery(params: PositionListParams) {
  return queryOptions({
    queryKey: [...positionsQueryKey, "list", params],
    queryFn: async () =>
      unwrap(await api.GET("/api/v1/positions", { params: { query: params } })),
    placeholderData: keepPreviousData,
  });
}

/** One position by id, never kept once nothing shows it, so a change made from it starts from the current version. */
export function positionQuery(id: string) {
  return queryOptions({
    queryKey: [...positionsQueryKey, "detail", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/positions/{id}", { params: { path: { id } } }),
      ),
    gcTime: 0,
  });
}

/** The positions of a unit's own list and of the lists of the units above it, ordered by name. */
export function unitPositionsQuery(orgUnitId: string) {
  return queryOptions({
    queryKey: [...unitPositionsQueryKey, orgUnitId],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/org-units/{id}/positions", {
          params: { path: { id: orgUnitId } },
        }),
      ),
  });
}

/** Reads how many working employees hold the position; never cached, because the confirmation must show the state now. */
export async function fetchPositionImpact(id: string) {
  return unwrap(
    await api.GET("/api/v1/positions/{id}/impact", {
      params: { path: { id } },
    }),
  );
}

/** Reads how many working employees of the unit and the units below it hold the position; never cached, like the other impacts. */
export async function fetchUnitPositionImpact(
  orgUnitId: string,
  positionId: string,
) {
  return unwrap(
    await api.GET("/api/v1/org-units/{id}/positions/{positionId}/impact", {
      params: { path: { id: orgUnitId, positionId } },
    }),
  );
}

/** Rereads the catalogue, the units' lists and the employees, which show the position names. */
export function invalidatePositions(queryClient: QueryClient) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: positionsQueryKey }),
    queryClient.invalidateQueries({ queryKey: unitPositionsQueryKey }),
    queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
  ]);
}
