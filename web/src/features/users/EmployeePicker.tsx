import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Flex, Select, Typography } from "antd";
import { useState } from "react";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { describeApiError } from "../../app/apiErrors";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import { employeesQueryKey } from "../org-structure/orgStructureApi";

const PICKER_PAGE_SIZE = 20;

interface EmployeeOption {
  value: string;
  label: string;
  detail?: string;
}

interface EmployeePickerProps {
  /** Element id that associates the form item label with the control; supplied by the surrounding form item. */
  id?: string;
  /** Identifier of the chosen employee; supplied by the surrounding form item. */
  value?: string;
  /** Receives the chosen identifier, or undefined when the choice is cleared; supplied by the surrounding form item. */
  onChange?: (value: string | undefined) => void;
  /** The employee already bound, listed by name even when the search does not return them. */
  current?: { id: string; name: string };
  /** Narrows the choice to the employees working directly in this unit, as for a unit's head; omitted, every working employee is offered. */
  orgUnitId?: string;
}

/** Remote-search select of working employees, all of them or those of one unit, showing each one's unit and position so namesakes can be told apart. */
export function EmployeePicker({
  id,
  value,
  onChange,
  current,
  orgUnitId,
}: EmployeePickerProps) {
  const [search, setSearch] = useState("");
  const text = useDebouncedValue(search.trim());
  const { data, error, isFetching } = useQuery({
    queryKey: [...employeesQueryKey, "picker", orgUnitId ?? null, text],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/employees", {
          params: {
            query: {
              q: text || undefined,
              isActive: true,
              orgUnitId,
              pageSize: PICKER_PAGE_SIZE,
            },
          },
        }),
      ),
    placeholderData: keepPreviousData,
  });
  const found: EmployeeOption[] = (data?.items ?? []).map((employee) => ({
    value: employee.id,
    label: employee.fullName,
    detail: `${employee.orgUnitName}, ${employee.position}`,
  }));
  const options =
    current && !found.some((option) => option.value === current.id)
      ? [{ value: current.id, label: current.name }, ...found]
      : found;

  return (
    <Select<string, EmployeeOption>
      id={id}
      showSearch
      allowClear
      filterOption={false}
      value={value}
      onChange={onChange}
      onSearch={setSearch}
      loading={isFetching}
      options={options}
      placeholder="Начните вводить ФИО сотрудника"
      notFoundContent={
        error
          ? describeApiError(error)
          : "Сотрудники не найдены. Измените запрос."
      }
      optionRender={(option) => (
        <Flex vertical>
          <span>{option.label}</span>
          {option.data.detail && (
            <Typography.Text type="secondary">
              {option.data.detail}
            </Typography.Text>
          )}
        </Flex>
      )}
    />
  );
}
