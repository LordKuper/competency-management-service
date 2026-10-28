import {
  Button,
  Flex,
  Select,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import { useState } from "react";
import type { EmployeeOption } from "./CommissionView";

interface RatersFieldProps {
  /** Identifiers of the chosen raters; supplied by the surrounding form item. */
  value?: string[];
  onChange?: (value: string[]) => void;
  /** Raters already on the list, so they are shown by name before any search. */
  initial: ReadonlyArray<{ employeeId: string; fullName: string }>;
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  /** People who cannot be offered, such as the candidate. */
  excludedIds?: readonly string[];
  isSearching: boolean;
  isLocked: boolean;
  onSearch: (text: string) => void;
}

interface Chosen {
  id: string;
  name: string;
  detail?: string;
}

/** The list of raters of a candidate: people are found by name and added one by one, and each can be taken off the list. */
export function RatersField({
  value = [],
  onChange,
  initial,
  candidates,
  excludedIds = [],
  isSearching,
  isLocked,
  onSearch,
}: RatersFieldProps) {
  const [known, setKnown] = useState<Record<string, Chosen>>(() =>
    Object.fromEntries(
      initial.map((rater) => [
        rater.employeeId,
        { id: rater.employeeId, name: rater.fullName },
      ]),
    ),
  );
  const options = candidates
    .filter(
      (candidate) =>
        !value.includes(candidate.id) && !excludedIds.includes(candidate.id),
    )
    .map((candidate) => ({ value: candidate.id, label: candidate.name }));
  const rows = value.flatMap((id) => (known[id] ? [known[id]] : []));

  const columns: TableColumnsType<Chosen> = [
    {
      title: "Оценивающий",
      key: "name",
      render: (_, rater) => (
        <Flex vertical>
          <span>{rater.name}</span>
          {rater.detail && (
            <Typography.Text type="secondary">{rater.detail}</Typography.Text>
          )}
        </Flex>
      ),
    },
    {
      title: "Действия",
      key: "actions",
      width: 1,
      render: (_, rater) => (
        <Button
          type="link"
          danger
          disabled={isLocked}
          aria-label={`Убрать ${rater.name}`}
          onClick={() => onChange?.(value.filter((id) => id !== rater.id))}
        >
          Убрать
        </Button>
      ),
    },
  ];

  return (
    <Flex vertical gap="middle">
      <Select<string | null>
        showSearch={{ filterOption: false, onSearch }}
        aria-label="Добавить оценивающего"
        placeholder="Добавить: начните вводить ФИО"
        value={null}
        options={options}
        loading={isSearching}
        disabled={isLocked}
        optionRender={(option) => (
          <Flex vertical>
            <span>{option.label}</span>
            <Typography.Text type="secondary">
              {
                candidates.find((candidate) => candidate.id === option.value)
                  ?.detail
              }
            </Typography.Text>
          </Flex>
        )}
        notFoundContent="Сотрудники не найдены. Измените запрос."
        onSelect={(id) => {
          const added = candidates.find((candidate) => candidate.id === id);
          if (!added || id === null) return;
          setKnown((current) => ({
            ...current,
            [added.id]: {
              id: added.id,
              name: added.name,
              detail: added.detail,
            },
          }));
          onChange?.([...value, id]);
        }}
      />
      <Table<Chosen>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        pagination={false}
        locale={{ emptyText: "Оценивающих пока нет." }}
      />
      <Typography.Text type="secondary">Выбрано: {rows.length}</Typography.Text>
    </Flex>
  );
}
