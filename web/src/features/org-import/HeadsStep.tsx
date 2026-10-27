import {
  Alert,
  Button,
  Flex,
  Radio,
  Select,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { useMemo, useState } from "react";
import { ApiError } from "../../api/ApiError";
import { describeApiError } from "../../app/apiErrors";
import { ActionBar, CompactAlert } from "../desk/ScenarioContext";
import { type HeadChoices, type HeadRow, headRows } from "./importHeads";
import type { KeptIds } from "./importPlan";
import type { OrgImportPlan } from "./orgImportApi";

const PAGE_SIZE = 50;
const NO_HEAD = "";
const ALL = "all";
const ATTENTION = "attention";

const pagination = {
  pageSize: PAGE_SIZE,
  hideOnSinglePage: true,
  showSizeChanger: false,
} as const;

interface HeadsStepProps {
  plan: OrgImportPlan;
  kept: KeptIds;
  choices: HeadChoices;
  /** Sets the head of a unit by its key; null leaves the unit without a head. */
  onChoose: (unitKey: string, employeeKey: string | null) => void;
  /** The apply is in flight: the choices stay as they are until it ends. */
  isApplying: boolean;
  /** Why the last apply was refused, other than a changed plan. */
  failure: unknown;
  onBack: () => void;
  onApply: () => void;
}

/** Why an apply was refused, in the server's words: a rule of the organization, or choices it could not read. */
function failureText(failure: unknown): string {
  if (failure instanceof ApiError && failure.status === 400) {
    const reasons = [
      ...(failure.errors?.decisions ?? []),
      ...(failure.errors?.heads ?? []),
    ];
    return reasons.length > 0 ? reasons.join(" ") : describeApiError(failure);
  }
  return describeApiError(failure);
}

/**
 * Step 4: the head of every unit of the file that has candidates or a head now, chosen from the employees who work in
 * the unit after the apply, which the decisions about the missing employees decide. The defaults follow those decisions.
 */
export function HeadsStep({
  plan,
  kept,
  choices,
  onChoose,
  isApplying,
  failure,
  onBack,
  onApply,
}: HeadsStepProps) {
  const [filter, setFilter] = useState(ALL);
  const rows = useMemo(
    () => headRows(plan, kept, choices),
    [plan, kept, choices],
  );
  const attention = useMemo(
    () => rows.filter((row) => row.needsAttention),
    [rows],
  );
  const shown = filter === ATTENTION ? attention : rows;

  const columns: TableColumnsType<HeadRow> = [
    {
      title: "Подразделение",
      key: "unit",
      render: (_value, row) => (
        <Flex vertical gap="var(--ant-margin-xxs)" align="flex-start">
          <span>{row.unit.path}</span>
          {row.unit.unitId === null && <Tag>Новое</Tag>}
        </Flex>
      ),
    },
    {
      title: "Сейчас",
      key: "current",
      width: "20%",
      render: (_value, row) =>
        row.currentHeadName === null ? (
          "—"
        ) : (
          <Flex vertical gap="var(--ant-margin-xxs)">
            <span>{row.currentHeadName}</span>
            {!row.isCurrentHeadStaying && (
              <Typography.Text type="secondary">
                Не остаётся в подразделении
              </Typography.Text>
            )}
          </Flex>
        ),
    },
    {
      title: "Руководитель",
      key: "head",
      width: "30%",
      render: (_value, row) => (
        <Select<string>
          aria-label={`Руководитель: ${row.unit.path}`}
          style={{ width: "100%" }}
          disabled={isApplying}
          showSearch
          optionFilterProp="label"
          status={row.isUndecided ? "warning" : undefined}
          placeholder="Выберите руководителя"
          value={row.isUndecided ? undefined : (row.chosen ?? NO_HEAD)}
          options={[
            { value: NO_HEAD, label: "Без руководителя" },
            ...row.options.map(({ key, fullName }) => ({
              value: key,
              label: fullName,
            })),
          ]}
          onChange={(value) =>
            onChoose(row.unit.key, value === NO_HEAD ? null : value)
          }
        />
      ),
    },
    {
      title: "Замечание",
      key: "remark",
      render: (_value, row) =>
        (row.isUndecided || row.mismatch) && (
          <Flex vertical gap="var(--ant-margin-xxs)" align="flex-start">
            {row.isUndecided && (
              <Tag color="warning">
                Несколько кандидатов — выберите руководителя
              </Tag>
            )}
            {row.mismatch && <span>{row.mismatch}</span>}
          </Flex>
        ),
    },
  ];

  return (
    <Flex vertical gap="middle">
      {rows.length === 0 ? (
        <CompactAlert
          type="info"
          lead="Руководителей назначать не нужно."
          text="В файле нет подразделений, где можно распознать руководителя, и у подразделений файла нет действующего руководителя."
        />
      ) : (
        <Flex vertical gap="small">
          <Flex justify="space-between" align="center" gap="small" wrap>
            <Typography.Title level={4} style={{ margin: 0 }}>
              {`Подразделений с руководителем: ${rows.length}`}
            </Typography.Title>
            <Radio.Group
              optionType="button"
              value={filter}
              options={[
                { value: ALL, label: `Все (${rows.length})` },
                {
                  value: ATTENTION,
                  label: `Требуют внимания (${attention.length})`,
                },
              ]}
              onChange={(event) => setFilter(event.target.value)}
            />
          </Flex>
          <Table<HeadRow>
            size="small"
            className="desk-table"
            rowKey={(row) => row.unit.key}
            columns={columns}
            dataSource={shown}
            pagination={pagination}
            locale={{ emptyText: "Нет подразделений, требующих внимания." }}
          />
        </Flex>
      )}
      {failure != null && (
        <Alert
          type="error"
          showIcon
          title="Импорт не применён"
          description={failureText(failure)}
        />
      )}
      <ActionBar
        start={
          <Button disabled={isApplying} onClick={onBack}>
            Назад
          </Button>
        }
      >
        <Button type="primary" loading={isApplying} onClick={onApply}>
          Применить
        </Button>
      </ActionBar>
    </Flex>
  );
}
