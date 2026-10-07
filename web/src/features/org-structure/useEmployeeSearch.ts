import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { type Employee, employeeSearchQuery } from "./orgStructureApi";

const SEARCH_MIN_LENGTH = 2;
const NO_EMPLOYEES: readonly Employee[] = [];

/** The employees a search text matches on the server. */
export interface EmployeeSearch {
  /** The matches shown, in list order; empty while the text is too short and before the first answer. */
  employees: readonly Employee[];
  /** How many employees match in all; more than the ones shown when the answer is cut at one page. */
  total: number;
  /** Whether the answer for the current text is still to come. */
  isWaiting: boolean;
  error: Error | null;
  retry: () => void;
}

/**
 * Looks employees up by the typed text once the typing pauses and the text is long enough to be worth a request; a
 * shorter text finds nobody. The matches of the previous text stay until the next answer arrives.
 */
export function useEmployeeSearch(text: string): EmployeeSearch {
  const typed = text.trim();
  const settled = useDebouncedValue(typed);
  const { data, error, isFetching, refetch } = useQuery({
    ...employeeSearchQuery(settled),
    enabled: settled.length >= SEARCH_MIN_LENGTH,
    placeholderData: keepPreviousData,
  });
  const isSearching = typed.length >= SEARCH_MIN_LENGTH;
  const found = isSearching ? data : undefined;

  return {
    employees: found?.items ?? NO_EMPLOYEES,
    total: found?.total ?? 0,
    isWaiting: isSearching && (settled !== typed || isFetching),
    error: isSearching ? error : null,
    retry: () => void refetch(),
  };
}
