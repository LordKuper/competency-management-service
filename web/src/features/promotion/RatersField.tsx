import {
  Button,
  Flex,
  Input,
  Select,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useState } from "react";
import type { EmployeeOption } from "./CommissionView";
import type { RaterDetails, RaterEntry } from "./promotionApi";
import { RATER_FORBIDDEN } from "./promotionTexts";
import {
  describeEmailFailure,
  isSameEmail,
  normalizedEmail,
} from "./raterEmail";

interface RatersFieldProps {
  /** The raters chosen with the address to invite each at; supplied by the surrounding form item. */
  value?: RaterEntry[];
  onChange?: (value: RaterEntry[]) => void;
  /** Raters the server already holds, so they are shown by name before any search and known to have an account or not. */
  saved: readonly RaterDetails[];
  /** Employees the last search found. */
  candidates: readonly EmployeeOption[];
  /** People who cannot be offered, such as the candidate. */
  excludedIds?: readonly string[];
  isSearching: boolean;
  isLocked: boolean;
  onSearch: (text: string) => void;
  /** Confirms the saved address of a rater; the action is offered only when given. A refusal is shown by the row. */
  onConfirmEmail?: (employeeId: string, email: string) => Promise<unknown>;
  /** Whether confirming waits, because the list holds edits that a reload of the saved raters would lose. */
  isConfirmLocked?: boolean;
  /** Shows who put each saved rater on the list, which the head of the unit needs to tell the candidate's choices from their own. */
  hasSourceColumn?: boolean;
}

const SOURCE_LABELS: Record<RaterDetails["source"], string> = {
  Candidate: "Кандидат",
  Head: "Руководитель",
};

const EMAIL_INPUT_STYLE = { minWidth: "calc(var(--ant-control-height) * 7)" };

interface Chosen {
  id: string;
  name: string;
  detail?: string;
  unitName?: string;
  hasAccount?: boolean;
}

/** The list of raters of a candidate: people are found by name and added one by one, each can be taken off the list, and one without an account gets the address to invite them at. */
export function RatersField({
  value = [],
  onChange,
  saved,
  candidates,
  excludedIds = [],
  isSearching,
  isLocked,
  onSearch,
  onConfirmEmail,
  isConfirmLocked = false,
  hasSourceColumn = false,
}: RatersFieldProps) {
  const [known, setKnown] = useState<Record<string, Chosen>>(() =>
    Object.fromEntries(
      saved.map((rater) => [
        rater.employeeId,
        {
          id: rater.employeeId,
          name: rater.fullName,
          hasAccount: rater.hasAccount,
        },
      ]),
    ),
  );
  const [failures, setFailures] = useState<Record<string, string>>({});
  const savedById = new Map(saved.map((rater) => [rater.employeeId, rater]));
  const entryById = new Map(value.map((entry) => [entry.employeeId, entry]));
  const options = candidates
    .filter(
      (candidate) =>
        !entryById.has(candidate.id) && !excludedIds.includes(candidate.id),
    )
    .map((candidate) => ({
      value: candidate.id,
      label: candidate.name,
      disabled: candidate.isForbidden === true,
    }));
  const rows = value.flatMap((entry) => {
    const rater = known[entry.employeeId];
    return rater ? [rater] : [];
  });

  function setEmail(id: string, email: string) {
    setFailures(({ [id]: _dropped, ...rest }) => rest);
    onChange?.(
      value.map((entry) =>
        entry.employeeId === id ? { ...entry, email } : entry,
      ),
    );
  }

  async function confirm(id: string, email: string) {
    try {
      await onConfirmEmail?.(id, email);
    } catch (error) {
      setFailures((current) => ({
        ...current,
        [id]: describeEmailFailure(error),
      }));
    }
  }

  function renderAccount(rater: Chosen) {
    const details = savedById.get(rater.id);
    if (details?.hasAccount ?? rater.hasAccount) {
      return <Tag color="success">Есть</Tag>;
    }
    const email = entryById.get(rater.id)?.email ?? "";
    const isSavedAddress =
      details !== undefined &&
      normalizedEmail(details.email) !== null &&
      isSameEmail(details.email, email);
    return (
      <Flex vertical gap="small">
        <Flex gap="small" align="center" wrap>
          <Input
            type="email"
            autoComplete="off"
            aria-label={`Почта: ${rater.name}`}
            placeholder="Почта для приглашения"
            value={email}
            disabled={isLocked}
            status={failures[rater.id] ? "error" : undefined}
            style={EMAIL_INPUT_STYLE}
            onChange={(event) => setEmail(rater.id, event.target.value)}
          />
          {canConfirm(rater.id) && (
            <Button
              type="primary"
              disabled={isLocked || isConfirmLocked}
              aria-label={`Подтвердить почту: ${rater.name}`}
              onClick={() =>
                void confirm(rater.id, entryById.get(rater.id)?.email ?? "")
              }
            >
              Подтвердить
            </Button>
          )}
        </Flex>
        {failures[rater.id] && (
          <Typography.Text type="danger">{failures[rater.id]}</Typography.Text>
        )}
        {isSavedAddress && (
          <Flex vertical>
            {details.isEmailConfirmed ? (
              <Tag color="success">Почта подтверждена</Tag>
            ) : (
              <Tag color="warning">Почта не подтверждена</Tag>
            )}
            {details.emailConfirmedByName && (
              <Typography.Text type="secondary">
                {details.emailConfirmedByName}
              </Typography.Text>
            )}
          </Flex>
        )}
      </Flex>
    );
  }

  function canConfirm(id: string): boolean {
    const details = savedById.get(id);
    return (
      onConfirmEmail !== undefined &&
      details !== undefined &&
      !details.hasAccount &&
      !details.isEmailConfirmed &&
      normalizedEmail(details.email) !== null &&
      isSameEmail(details.email, entryById.get(id)?.email)
    );
  }

  const columns: TableColumnsType<Chosen> = [
    {
      title: "Сотрудник",
      key: "name",
      render: (_, rater) => (
        <Flex vertical>
          <span>{rater.name}</span>
          {rater.detail && (
            <Typography.Text type="secondary">{rater.detail}</Typography.Text>
          )}
          {savedById.get(rater.id)?.isExcluded && (
            <Flex>
              <Tag color="warning">{RATER_FORBIDDEN}</Tag>
            </Flex>
          )}
        </Flex>
      ),
    },
    {
      title: "Подразделение",
      key: "unit",
      render: (_, rater) =>
        savedById.get(rater.id)?.unitName ?? rater.unitName ?? "—",
    },
    ...(hasSourceColumn
      ? [
          {
            title: "Добавил",
            key: "source",
            render: (_: unknown, rater: Chosen) => {
              const source = savedById.get(rater.id)?.source;
              return source ? SOURCE_LABELS[source] : "—";
            },
          },
        ]
      : []),
    {
      title: "Учётная запись",
      key: "account",
      render: (_, rater) => renderAccount(rater),
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
          aria-label={`Убрать: ${rater.name}`}
          onClick={() =>
            onChange?.(value.filter((entry) => entry.employeeId !== rater.id))
          }
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
        optionRender={(option) => {
          const found = candidates.find(
            (candidate) => candidate.id === option.value,
          );
          return (
            <Flex justify="space-between" align="center" gap="small">
              <Flex vertical>
                <span>{option.label}</span>
                <Typography.Text type="secondary">
                  {found?.detail}
                </Typography.Text>
              </Flex>
              {found?.isForbidden && (
                <Tag color="warning">{RATER_FORBIDDEN}</Tag>
              )}
            </Flex>
          );
        }}
        notFoundContent="Сотрудники не найдены. Измените запрос."
        onSelect={(id) => {
          onSearch("");
          const added = candidates.find((candidate) => candidate.id === id);
          if (!added || id === null) return;
          setKnown((current) => ({
            ...current,
            [added.id]: {
              id: added.id,
              name: added.name,
              detail: added.detail,
              unitName: added.unitName,
              hasAccount: added.hasAccount,
            },
          }));
          onChange?.([...value, { employeeId: id, email: null }]);
        }}
      />
      <Table<Chosen>
        rowKey="id"
        columns={columns}
        dataSource={rows}
        pagination={false}
        scroll={{ x: "max-content" }}
        locale={{ emptyText: "Оценивающих пока нет." }}
      />
    </Flex>
  );
}
