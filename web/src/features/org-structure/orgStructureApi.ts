import {
  keepPreviousData,
  type QueryClient,
  queryOptions,
} from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** A unit as the API returns it in lists and on its own, and from the operations that change it. */
export type OrgUnit = components["schemas"]["OrgUnitResponse"];

/** A unit of the tree with the head's name and the number of employees directly in it, as the signed-in user may see them. */
export type OrgUnitTreeNode = components["schemas"]["OrgUnitTreeNodeResponse"];

/** An employee; the projection for ordinary users leaves out the status and the version. */
export type Employee = components["schemas"]["EmployeeResponse"];

/** One assignment of an employee: a unit, a position there and the legal entities it is held for; the main one is not secondary. */
export type EmployeeAssignment = components["schemas"]["AssignmentResponse"];

/** A legal entity of the catalogue. */
export type LegalEntity = components["schemas"]["LegalEntityResponse"];

/** What dismissing or deleting an employee changes elsewhere: the units that lose their head and the account that is blocked, or the refusal when that account is the last active administrator. */
export type EmployeeImpact = components["schemas"]["EmployeeImpactResponse"];

/** The fields an employee is created or replaced with. */
export type EmployeeInput = components["schemas"]["EmployeeRequest"];

/** Cache key prefix of every legal entity query. */
const legalEntitiesQueryKey = ["legal-entities"] as const;

/** Cache key prefix of every unit query, so one invalidation rereads the tree. */
export const orgUnitsQueryKey = ["org-units"] as const;

/** Cache key prefix of every employee query, the employee picker of the account screens included. */
export const employeesQueryKey = ["employees"] as const;

/**
 * Reread policy of the unit and employee lists: a change made here rereads them explicitly, a list shown again within a
 * minute is not requested again, and returning to the browser tab does not reread them, which would repeat the request
 * of every open unit. A save based on an older list is refused by its version.
 */
const LIST_REREAD = { staleTime: 60_000, refetchOnWindowFocus: false } as const;

/** One unit by id, never kept once nothing shows it, so a change made from it always starts from the current version. */
export function orgUnitQuery(id: string) {
  return queryOptions({
    queryKey: [...orgUnitsQueryKey, "detail", id],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/org-units/{id}", { params: { path: { id } } }),
      ),
    gcTime: 0,
  });
}

/** Every unit visible to the signed-in user as a flat list; the tree is assembled from the parent links. */
export const orgUnitTreeQuery = queryOptions({
  queryKey: [...orgUnitsQueryKey, "tree"],
  queryFn: async () => unwrap(await api.GET("/api/v1/org-units/tree")),
  ...LIST_REREAD,
});

/** Largest page the employee list accepts, as `ListQueries.MaxPageSize` in the API; the OpenAPI document does not carry the limit. */
const EMPLOYEE_PAGE_SIZE_MAX = 200;

/** Longest search text the employee list accepts, as `TextSearch.MaxLength` in the API; the OpenAPI document does not carry the limit. */
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
    ...LIST_REREAD,
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
    ...LIST_REREAD,
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

/** Legal entities listed for one search; a longer list is narrowed by typing more. */
const LEGAL_ENTITY_PAGE_SIZE = 50;

/** The first page of the legal entities whose name contains the text, in list order; the whole first page when the text is blank. */
export function legalEntitiesQuery(text: string) {
  return queryOptions({
    queryKey: [...legalEntitiesQueryKey, text],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/legal-entities", {
          params: { query: { q: text, pageSize: LEGAL_ENTITY_PAGE_SIZE } },
        }),
      ),
    ...LIST_REREAD,
    placeholderData: keepPreviousData,
  });
}

/** Finds the legal entity of that name, whatever the letter case, or creates it. */
export async function quickAddLegalEntity(name: string) {
  return unwrap(
    await api.POST("/api/v1/legal-entities/quick-add", { body: { name } }),
  );
}

/** Rereads the legal entity lists after one was added. */
export function invalidateLegalEntities(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: legalEntitiesQueryKey });
}

/** The main assignment of an employee in the shape of the others. */
export function mainAssignmentOf(employee: Employee): EmployeeAssignment {
  return {
    orgUnitId: employee.orgUnitId,
    orgUnitName: employee.orgUnitName,
    positionId: employee.positionId,
    position: employee.position,
    legalEntities: employee.legalEntities,
    isSecondary: false,
  };
}

/**
 * The input that saves the employee exactly as the server holds them. A change is spread over it, because a save replaces
 * the whole set of assignments and whatever a change leaves out would be removed.
 */
export function employeeInputOf(employee: Employee): EmployeeInput {
  return {
    lastName: employee.lastName,
    firstName: employee.firstName,
    middleName: employee.middleName ?? undefined,
    orgUnitId: employee.orgUnitId,
    positionId: employee.positionId,
    legalEntityIds: employee.legalEntities.map(({ id }) => id),
    assignments: employee.assignments.map((assignment) => ({
      orgUnitId: assignment.orgUnitId,
      positionId: assignment.positionId,
      legalEntityIds: assignment.legalEntities.map(({ id }) => id),
    })),
  };
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
