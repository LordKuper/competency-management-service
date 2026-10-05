import { useQuery } from "@tanstack/react-query";
import { api } from "../../api/client";
import type { components } from "../../api/schema";
import { unwrap } from "../../api/unwrap";

/** The signed-in account as `GET /api/v1/auth/me` describes it. */
export type CurrentUser = components["schemas"]["CurrentUserResponse"];

/** Cache key of the signed-in account: sign-in seeds it, and sign-out and an ended session clear it. */
export const currentUserQueryKey = ["auth", "me"] as const;

/** The signed-in account; a 401 is not retried and sends the app to the sign-in screen through the single unauthorized handler. */
export function useCurrentUser() {
  return useQuery({
    queryKey: currentUserQueryKey,
    queryFn: async () => unwrap(await api.GET("/api/v1/auth/me")).data,
  });
}

/** Whether the signed-in account is a global administrator; false while loading and when signed out. */
export function useIsAdmin(): boolean {
  return useCurrentUser().data?.role === "GlobalAdmin";
}
