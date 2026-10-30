import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { employeesQueryKey } from "../org-structure/orgStructureApi";
import type { EmployeeOption } from "./CommissionView";

const OPTIONS_PAGE_SIZE = 20;

/** Working employees matching the typed text, as a screen offers them to add to a list; `purpose` keeps the cache of one screen apart. */
export function useEmployeeOptions(purpose: string) {
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const found = useQuery({
    queryKey: [...employeesQueryKey, purpose, text],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees", {
          params: {
            query: {
              q: text || undefined,
              isActive: true,
              pageSize: OPTIONS_PAGE_SIZE,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  });
  const candidates: EmployeeOption[] = (found.data?.items ?? []).map(
    (employee) => ({
      id: employee.id,
      name: employee.fullName,
      detail: `${employee.orgUnitName}, ${employee.position}`,
      unitName: employee.orgUnitName,
      hasAccount: employee.hasAccount,
    }),
  );
  return {
    candidates,
    isSearching: found.isFetching || search.trim() !== text,
    onSearch: setSearch,
  };
}
