import { ApiError } from "./ApiError";

/** Successful payload with the `ETag` that must accompany the next write to the same resource. */
export interface Versioned<TData> {
  data: TData;
  etag: string | null;
}

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
): Versioned<Exclude<TResult["data"], undefined>> {
  if (result.error !== undefined) {
    throw new ApiError(result.response.status, result.error);
  }
  return {
    data: result.data as Exclude<TResult["data"], undefined>,
    etag: result.response.headers.get("ETag"),
  };
}
