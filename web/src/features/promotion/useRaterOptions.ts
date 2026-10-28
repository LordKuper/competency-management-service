import { useQuery } from "@tanstack/react-query";
import { excludedRatersQuery } from "./promotionApi";
import { useEmployeeOptions } from "./useEmployeeOptions";

/** Employees matching the typed text as a screen offers them to add as raters, with the ones the administrator forbids marked; they are offered only once the check of the found people is done. */
export function useRaterOptions(purpose: string) {
  const options = useEmployeeOptions(purpose);
  const forbidden = useQuery(
    excludedRatersQuery(options.candidates.map((candidate) => candidate.id)),
  );
  const isChecking = forbidden.isPending && forbidden.fetchStatus !== "idle";
  const forbiddenIds = new Set(forbidden.data);
  return {
    candidates: isChecking
      ? []
      : options.candidates.map((candidate) => ({
          ...candidate,
          isForbidden: forbiddenIds.has(candidate.id),
        })),
    isSearching: options.isSearching || isChecking,
    onSearch: options.onSearch,
  };
}
