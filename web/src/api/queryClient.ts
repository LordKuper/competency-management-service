import { MutationCache, QueryClient } from "@tanstack/react-query";
import { ApiError } from "./ApiError";

const MAX_RETRIES = 3;

/** Prefix of every desk query: any successful change may add or remove the user's expected actions. */
export const deskQueryKey = ["desk"] as const;

/** Retries transient failures only: a 4xx answer repeats identically, so repeating it just delays the error. */
export function shouldRetry(failureCount: number, error: Error): boolean {
  const isClientError =
    error instanceof ApiError && error.status >= 400 && error.status < 500;
  return !isClientError && failureCount < MAX_RETRIES;
}

/**
 * Shared server-state cache. Every successful mutation rereads the desk without waiting for it, so no mutation stays
 * pending while the server recomputes the desk.
 */
export const queryClient: QueryClient = new QueryClient({
  defaultOptions: { queries: { retry: shouldRetry } },
  mutationCache: new MutationCache({
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: deskQueryKey });
    },
  }),
});
