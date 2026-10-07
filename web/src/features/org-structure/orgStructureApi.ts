import { type QueryClient, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** A unit as the API returns it in lists and on its own, and from the operations that change it. */
export type OrgUnit = components["schemas"]["OrgUnitResponse"];

/** A unit of the tree with the head's name and the number of employees directly in it, as the signed-in user may see them. */
export type OrgUnitTreeNode = components["schemas"]["OrgUnitTreeNodeResponse"];

/** An employee; the projection for ordinary users leaves out the status and the version. */
export type Employee = components["schemas"]["EmployeeResponse"];

/** What dismissing or deleting an employee changes elsewhere: the units that lose their head and the account that is blocked, or the refusal when that account is the last active administrator. */
export type EmployeeImpact = components["schemas"]["EmployeeImpactResponse"];

/** The fields an employee is created or replaced with. */
export type EmployeeInput = components["schemas"]["EmployeeRequest"];

/** Cache key prefix of every unit query, so one invalidation rereads the tree. */
export const orgUnitsQueryKey = ["org-units"] as const;

/** Cache key prefix of every employee query, the employee picker of the account screens included. */
export const employeesQueryKey = ["employees"] as const;

/** Every unit visible to the signed-in user as a flat list; the tree is assembled from the parent links. */
export const orgUnitTreeQuery = queryOptions({
  queryKey: [...orgUnitsQueryKey, "tree"],
  queryFn: async () => unwrap(await api.GET("/api/v1/org-units/tree")),
});

/** Largest page the employee list accepts. */
const EMPLOYEE_PAGE_SIZE_MAX = 200;

/** Longest search text the employee list accepts. */
export const SEARCH_TEXT_MAX_LENGTH = 200;

/** The first page of the employees whose name or position match the text, in list order, with the number of matches in all. */
export function employeeSearchQuery(text: string) {
  return queryOptions({
    queryKey: [...employeesQueryKey, "search", text],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees", {
          params: { query: { q: text, pageSize: EMPLOYEE_PAGE_SIZE_MAX } },
        }),
      ),
  });
}

/** Every employee directly in a unit, in list order: the first page, then the remaining pages at once. */
export function unitEmployeesQuery(orgUnitId: string) {
  return queryOptions({
    queryKey: [...employeesQueryKey, "unit", orgUnitId],
    queryFn: async () => {
      const readPage = async (page: number) =>
        unwrap(
          await api.GET("/api/v1/employees", {
            params: {
              query: { orgUnitId, page, pageSize: EMPLOYEE_PAGE_SIZE_MAX },
            },
          }),
        );
      const first = await readPage(1);
      const lastPage = Math.ceil(first.total / first.pageSize);
      const rest = await Promise.all(
        Array.from({ length: Math.max(lastPage - 1, 0) }, (_, index) =>
          readPage(index + 2),
        ),
      );
      return [first, ...rest].flatMap(({ items }) => items);
    },
  });
}

/** One employee by id, in the projection the signed-in user's role allows; dropped once nothing shows it, so an edit dialog never opens on an older version. */
export function employeeQuery(id: string) {
  return queryOptions({
    queryKey: [...employeesQueryKey, "detail", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees/{id}", { params: { path: { id } } }),
      ),
    gcTime: 0,
  });
}

/** Reads what dismissing or deleting the employee would change elsewhere; never cached, because the confirmation must show the state now. */
export async function fetchEmployeeImpact(id: string) {
  return unwrap(
    await api.GET("/api/v1/employees/{id}/impact", {
      params: { path: { id } },
    }),
  );
}

/** Rereads every unit and employee query: a change to one reshapes the other's lists, names and counts. */
export function invalidateOrgStructure(queryClient: QueryClient) {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: orgUnitsQueryKey }),
    queryClient.invalidateQueries({ queryKey: employeesQueryKey }),
  ]);
}
