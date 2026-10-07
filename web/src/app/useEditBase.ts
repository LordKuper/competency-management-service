import { useState } from "react";
import { ApiError } from "../api/ApiError";

const STALE_VERSION_STATUS = 412;

/**
 * The record an edit form is based on. It stays as it was when the dialog opened, whatever the cache refetches in the
 * background, so unsaved input is never replaced and the version sent with a save is the one the form was built from.
 * It moves to the latest record only after a save was refused for a stale version and the cache then delivered a
 * newer one; the caller reports each failed save through `noteSaveFailure`.
 */
export function useEditBase<TRecord extends { version: number }>(
  latest: TRecord | undefined,
) {
  const [base, setBase] = useState(latest);
  const [isRefused, setIsRefused] = useState(false);

  if (isRefused && latest && latest.version !== base?.version) {
    setBase(latest);
    setIsRefused(false);
  }

  function noteSaveFailure(error: unknown) {
    if (error instanceof ApiError && error.status === STALE_VERSION_STATUS) {
      setIsRefused(true);
    }
  }

  return { base, noteSaveFailure };
}
