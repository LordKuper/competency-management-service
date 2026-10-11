import { useQuery } from "@tanstack/react-query";
import type { FilterValue } from "antd/es/table/interface";
import { type Dispatch, type SetStateAction, useMemo } from "react";
import { orgUnitTreeQuery } from "../org-structure/orgStructureApi";
import { buildUnitTree, type UnitNode } from "../org-structure/orgTree";

/** One value a column filter offers. */
export interface FilterOption {
  text: string;
  value: string | boolean;
}

/**
 * Column props of a one-value filter whose state lives in the query params: `current` is the param, so the filter shows
 * what the server was asked for. The column `key` must be the name of the param. `searchable` adds a search box to a long list.
 * The dropdown is widened so that long names (units) are not cut.
 */
export function choiceFilter(
  key: string,
  current: string | boolean | undefined,
  filters: FilterOption[],
  searchable = false,
) {
  return {
    key,
    filters,
    filteredValue: current === undefined ? null : [current],
    filterMultiple: false,
    filterSearch: searchable,
    filterDropdownProps: { styles: { root: { minWidth: 360 } } },
  };
}

/**
 * The table `onChange` of a table whose filters are query params: a changed filter is copied into the params under the
 * column key and the list goes back to page 1. Paging and sorting events are left to the table's own handlers.
 */
export function onFilterChange<P extends object>(
  setParams: Dispatch<SetStateAction<P>>,
) {
  return (
    _pagination: unknown,
    filters: Record<string, FilterValue | null>,
    _sorter: unknown,
    extra: { action: string },
  ) => {
    if (extra.action !== "filter") return;
    setParams((previous) => ({
      ...previous,
      ...Object.fromEntries(
        Object.entries(filters).map(([key, values]) => [key, values?.[0]]),
      ),
      ...("page" in previous && { page: 1 }),
    }));
  };
}

/** Options naming entries by their id. */
export function entryOptions(
  items: readonly { id: string; name: string }[] | undefined,
): FilterOption[] {
  return (items ?? []).map(({ id, name }) => ({ text: name, value: id }));
}

function flatten(nodes: readonly UnitNode[]): FilterOption[] {
  return nodes.flatMap(({ unit, children }) => [
    { text: unit.name, value: unit.id },
    ...flatten(children),
  ]);
}

/** Every visible unit in tree order, for a unit filter. */
export function useUnitOptions(): FilterOption[] {
  const { data } = useQuery(orgUnitTreeQuery);
  return useMemo(() => flatten(buildUnitTree(data ?? [])), [data]);
}
