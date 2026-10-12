import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Select, type SelectProps, theme } from "antd";
import type { FilterDropdownProps } from "antd/es/table/interface";
import { useState } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { type CatalogSegment, catalogQueryKey, entryQuery } from "./catalogApi";

const PICKER_PAGE_SIZE = 20;
const FILTER_WIDTH_IN_CONTROLS = 11;

/** Catalogues a picker chooses from. */
export type PickedSegment = Exclude<CatalogSegment, "levels">;

/** Further filters of the list a picker searches, by query parameter: the direction of tracks, the scale of competencies. */
export type PickerFilters = Record<string, string | boolean | undefined>;

/** The filter of a picker of entries to use: the inactive ones are left out. */
export const IN_WORK: PickerFilters = { inWork: true };

export interface PickerOption {
  value: string;
  label: string;
}

/**
 * Server search of a catalogue as the user types, one page of the first matches by name. The chosen entry is listed by
 * its name even when the search does not return it, as one deactivated since it was chosen.
 */
export function useCatalogSearch(
  segment: PickedSegment,
  value: string | undefined,
  filters: PickerFilters = {},
  isEnabled = true,
) {
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const query = {
    ...filters,
    q: text || undefined,
    page: 1,
    pageSize: PICKER_PAGE_SIZE,
  };
  const found = useQuery({
    queryKey: [...catalogQueryKey, segment, "search", query],
    queryFn: async () =>
      unwrap(
        await api.GET(
          `/api/v1/career/${segment}` as "/api/v1/career/directions",
          { params: { query: query as never } },
        ),
      ),
    placeholderData: keepPreviousData,
    enabled: isEnabled,
  });
  const items = found.data?.items ?? [];
  const isChosenListed = !value || items.some((item) => item.id === value);
  const chosen = useQuery({
    ...entryQuery(segment, value ?? ""),
    enabled: !isChosenListed,
  });
  const options: PickerOption[] = [
    ...(chosen.data && !isChosenListed ? [chosen.data] : []),
    ...items,
  ].map((entry) => ({ value: entry.id, label: entry.name }));
  return {
    search,
    setSearch,
    options,
    isFetching: found.isFetching || chosen.isFetching,
    error: found.error,
  };
}

/** `id`, `value` and `onChange` are supplied by the surrounding form item. */
interface CatalogSelectProps
  extends Pick<
    SelectProps<string, PickerOption>,
    | "id"
    | "value"
    | "onChange"
    | "allowClear"
    | "disabled"
    | "placeholder"
    | "style"
    | "getPopupContainer"
    | "aria-label"
  > {
  segment: PickedSegment;
  /** Further filters; a picker of entries to use leaves out the inactive ones with `inWork`. */
  filters?: PickerFilters;
  /** Entries not offered, such as those already used. */
  exclude?: readonly string[];
}

/** Choice of a catalogue entry with server search as the user types. */
export function CatalogSelect({
  segment,
  value,
  filters,
  exclude = [],
  disabled,
  ...props
}: CatalogSelectProps) {
  const { setSearch, options, isFetching, error } = useCatalogSearch(
    segment,
    value ?? undefined,
    filters,
    !disabled,
  );
  return (
    <Select<string, PickerOption>
      {...props}
      value={value}
      disabled={disabled}
      showSearch={{ filterOption: false, onSearch: setSearch }}
      onOpenChange={(isOpen) => {
        if (!isOpen) setSearch("");
      }}
      loading={isFetching}
      options={options.filter((option) => !exclude.includes(option.value))}
      notFoundContent={error ? describeApiError(error) : undefined}
    />
  );
}

interface CatalogFilterProps extends FilterDropdownProps {
  segment: PickedSegment;
  label: string;
}

/**
 * Filter dropdown of a column naming a catalogue entry; choosing one applies the filter at once. The list opens inside
 * the dropdown, because a click in a popup outside it would close the dropdown.
 */
function CatalogFilter({
  segment,
  label,
  selectedKeys,
  setSelectedKeys,
  confirm,
}: CatalogFilterProps) {
  const { token } = theme.useToken();
  return (
    <div style={{ padding: token.paddingXS }}>
      <CatalogSelect
        segment={segment}
        aria-label={label}
        allowClear
        value={selectedKeys[0] as string | undefined}
        style={{ width: token.controlHeight * FILTER_WIDTH_IN_CONTROLS }}
        getPopupContainer={(node) => node.parentElement ?? document.body}
        onChange={(value) => {
          setSelectedKeys(value ? [value] : []);
          confirm();
        }}
      />
    </div>
  );
}

/**
 * Column props of a filter by a catalogue entry whose state lives in the query params, like `choiceFilter`, with server
 * search instead of a list of every entry. The column `key` must be the name of the param.
 */
export function catalogFilter(
  key: string,
  segment: PickedSegment,
  current: string | undefined,
  label: string,
) {
  return {
    key,
    filteredValue: current === undefined ? null : [current],
    filterMultiple: false,
    filterDropdown: (props: FilterDropdownProps) => (
      <CatalogFilter {...props} segment={segment} label={label} />
    ),
  };
}
