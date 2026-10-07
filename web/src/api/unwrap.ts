import { ApiError } from "./ApiError";

interface ClientResult {
  data?: unknown;
  error?: unknown;
  response: Response;
}

/**
 * Turns an `openapi-fetch` result into the value a `queryFn` or `mutationFn` returns.
 * The client does not throw on 4xx/5xx, so the failure is raised here as an {@link ApiError}.
 */
export function unwrap<TResult extends ClientResult>(
  result: TResult,
): Exclude<TResult["data"], undefined> {
  if (result.error !== undefined) {
    throw new ApiError(result.response.status, result.error);
  }
  return result.data as Exclude<TResult["data"], undefined>;
}
