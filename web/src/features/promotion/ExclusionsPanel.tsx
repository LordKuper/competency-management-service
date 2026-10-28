import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Skeleton } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { positionListQuery } from "../positions/positionsApi";
import { ExclusionsView } from "./ExclusionsView";
import {
  addExcludedEmployee,
  addExcludedPosition,
  promotionQueryKey,
  raterExclusionsQuery,
  removeExcludedEmployee,
  removeExcludedPosition,
} from "./promotionApi";
import { useEmployeeOptions } from "./useEmployeeOptions";

const POSITION_OPTIONS_PAGE_SIZE = 20;

/** The lists of people and positions that cannot be named as raters, read and changed one entry at a time. */
export function ExclusionsPanel() {
  const queryClient = useQueryClient();
  const { data, error, isPending, refetch } = useQuery(raterExclusionsQuery);
  const employees = useEmployeeOptions("exclusion-picker");
  const [positionSearch, setPositionSearch] = useState("");
  const positionText = useDebouncedValue(positionSearch.trim());
  const found = useQuery({
    ...positionListQuery({
      q: positionText || undefined,
      isActive: true,
      page: 1,
      pageSize: POSITION_OPTIONS_PAGE_SIZE,
    }),
    placeholderData: keepPreviousData,
  });
  const change = useMutation({
    mutationFn: (apply: () => Promise<unknown>) => apply(),
    onSettled: () =>
      queryClient.invalidateQueries({ queryKey: promotionQueryKey }),
  });
  if (isPending) return <Skeleton active />;
  if (error) {
    return (
      <ErrorAlert
        title="Не удалось загрузить списки запрета"
        error={error}
        onRetry={() => void refetch()}
      />
    );
  }
  return (
    <>
      {change.isError && (
        <ErrorAlert title="Список не изменён" error={change.error} />
      )}
      <ExclusionsView
        exclusions={data}
        employees={employees.candidates}
        isSearchingEmployees={employees.isSearching}
        onSearchEmployees={employees.onSearch}
        positions={found.data?.items ?? []}
        isSearchingPositions={
          found.isFetching || positionSearch.trim() !== positionText
        }
        onSearchPositions={setPositionSearch}
        isBusy={change.isPending}
        onAddEmployee={(id) => change.mutate(() => addExcludedEmployee(id))}
        onRemoveEmployee={(id) =>
          change.mutate(() => removeExcludedEmployee(id))
        }
        onAddPosition={(id) => change.mutate(() => addExcludedPosition(id))}
        onRemovePosition={(id) =>
          change.mutate(() => removeExcludedPosition(id))
        }
      />
    </>
  );
}
