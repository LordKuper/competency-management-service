import type { FilterValue } from "antd/es/table/interface";
import type { Dispatch, SetStateAction } from "react";

/** One value a column filter offers. */
export interface FilterOption {
  text: string;
  value: string | boolean;
}

/**
 * Column props of a one-value filter whose state lives in the query params: `current` is the param, so the filter shows
 * what the server was asked for. The column `key` must be the name of the param.
 * The dropdown is widened so that long names are not cut.
 *
 */
export function choiceFilter(
  key: string,
  current: string | boolean | undefined,
  filters: FilterOption[],
) {
  return {
    key,
    filters,
    filteredValue: current === undefined ? null : [current],
    filterMultiple: false,
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

/** The table `pagination` of a list whose page lives in the query params. */
export function pageProps<P extends { page: number; pageSize: number }>(
  params: P,
  total: number | undefined,
  setParams: Dispatch<SetStateAction<P>>,
) {
  return {
    current: params.page,
    pageSize: params.pageSize,
    total: total ?? 0,
    showSizeChanger: true,
    onChange: (page: number, pageSize: number) =>
      setParams((previous) => ({ ...previous, page, pageSize })),
  };
}
