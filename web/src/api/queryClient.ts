import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "./ApiError";

const MAX_RETRIES = 3;

/** Retries transient failures only: a 4xx answer repeats identically, so repeating it just delays the error. */
export function shouldRetry(failureCount: number, error: Error): boolean {
  const isClientError =
    error instanceof ApiError && error.status >= 400 && error.status < 500;
  return !isClientError && failureCount < MAX_RETRIES;
}

/** Shared server-state cache. */
export const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: shouldRetry } },
});
