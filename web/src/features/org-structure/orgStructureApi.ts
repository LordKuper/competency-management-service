import {
  keepPreviousData,
  type QueryClient,
  queryOptions,
} from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** A unit as the API returns it, in the tree, in lists and on its own. */
export type OrgUnit = components["schemas"]["OrgUnitResponse"];

/** An employee; the projection for ordinary users leaves out the personnel number, the status and the version. */
export type Employee = components["schemas"]["EmployeeResponse"];

/** The fields an employee is created or replaced with. */
export type EmployeeInput = components["schemas"]["EmployeeRequest"];

/** Filters and paging of the employee list; pages count from 1. */
export interface EmployeeListParams {
  q?: string;
  isActive?: boolean;
  orgUnitId?: string;
  includeDescendants?: boolean;
  page: number;
  pageSize: number;
}

/** Cache key prefix of every unit query, so one invalidation rereads the tree, the open card and its counts together. */
export const orgUnitsQueryKey = ["org-units"] as const;

/** Cache key prefix of every employee query, the employee picker of the account screens included. */
export const employeesQueryKey = ["employees"] as const;

/** Every unit visible to the signed-in user as a flat list; the tree is assembled from the parent links. */
export const orgUnitTreeQuery = queryOptions({
  queryKey: [...orgUnitsQueryKey, "tree"],
  queryFn: async () => unwrap(await api.GET("/api/v1/org-units/tree")).data,
});

/** The units from the root down to the given unit, which comes last. */
export function orgUnitPathQuery(id: string) {
  return queryOptions({
    queryKey: [...orgUnitsQueryKey, "path", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/org-units/{id}/path", {
          params: { path: { id } },
        }),
      ).data,
  });
}

/** Working employees of a unit, directly and with all units below it. */
export function orgUnitSummaryQuery(id: string) {
  return queryOptions({
    queryKey: [...orgUnitsQueryKey, "summary", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/org-units/{id}/summary", {
          params: { path: { id } },
        }),
      ).data,
  });
}

/** One page of employees; the previous page stays on screen while the next loads. */
export function employeeListQuery(params: EmployeeListParams) {
  return queryOptions({
    queryKey: [...employeesQueryKey, "list", params],
    queryFn: async () =>
      unwrap(await api.GET("/api/v1/employees", { params: { query: params } }))
        .data,
    placeholderData: keepPreviousData,
  });
}

/** One employee by id, in the projection the signed-in user's role allows. */
export function employeeQuery(id: string) {
  return queryOptions({
    queryKey: [...employeesQueryKey, "detail", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees/{id}", { params: { path: { id } } }),
      ).data,
  });
}

/** Rereads every unit and employee query: a change to one reshapes the other's lists, names and counts. */
export function invalidateOrgStructure(queryClient: QueryClient) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: orgUnitsQueryKey }),
    queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
  ]);
}
