import { keepPreviousData, queryOptions } from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";
import type { UserRole } from "../../app/featureContract";

/** An account as the administrator API returns it. */
export type UserResponse = components["schemas"]["UserResponse"];

/** Filters and paging of the account list; pages count from 1. */
export interface UserListParams {
  q?: string;
  role?: UserRole;
  isBlocked?: boolean;
  isInvited?: boolean;
  page: number;
  pageSize: number;
}

/** Cache key prefix of every account query, so one invalidation rereads the list and the account open for editing together. */
export const usersQueryKey = ["users"] as const;

/** One page of accounts; the previous page stays on screen while the next loads. */
export function userListQuery(params: UserListParams) {
  return queryOptions({
    queryKey: [...usersQueryKey, "list", params],
    queryFn: async () =>
      unwrap(await api.GET("/api/v1/users", { params: { query: params } })),
    placeholderData: keepPreviousData,
  });
}

/** One account by id; dropped once nothing shows it, so reopening never starts from an older version. */
export function userQuery(id: string) {
  return queryOptions({
    queryKey: [...usersQueryKey, "detail", id],
    queryFn: async () =>
      unwrap(await api.GET("/api/v1/users/{id}", { params: { path: { id } } })),
    gcTime: 0,
  });
}
