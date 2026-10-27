import {
  Button,
  Flex,
  Radio,
  Table,
  type TableColumnsType,
  Tag,
  Typography,
} from "antd";
import { type ReactNode, useMemo } from "react";
import { ActionBar, CompactAlert } from "../desk/ScenarioContext";
import { isEmployeeKept, type KeptIds, unitBlocks } from "./importPlan";
import type {
  MissingEmployee,
  MissingUnit,
  OrgImportPlan,
} from "./orgImportApi";
import "./orgImport.css";

const PAGE_SIZE = 50;
const NO_UNITS = "—";
const ACT = "act";
const KEEP = "keep";

const UNIT_OPTIONS = [
  { value: ACT, label: "Деактивировать" },
  { value: KEEP, label: "Оставить" },
];
const EMPLOYEE_OPTIONS = [
  { value: ACT, label: "Уволить" },
  { value: KEEP, label: "Оставить" },
];

const pagination = {
  pageSize: PAGE_SIZE,
  hideOnSinglePage: true,
  showSizeChanger: false,
} as const;

interface MissingStepProps {
  plan: OrgImportPlan;
  kept: KeptIds;
  /** Sets the decision of the given entities: kept, or deactivated and dismissed. */
  onDecide: (ids: readonly string[], isKept: boolean) => void;
  onBack: () => void;
  onNext: () => void;
}

/**
 * Step 3: the units and employees the system has and the file does not, each deactivated or dismissed unless kept. A
 * decision the organization's rules refuse is marked before the apply, and the next step stays off while one is.
 */
export function MissingStep({
  plan,
  kept,
  onDecide,
  onBack,
  onNext,
}: MissingStepProps) {
  const blocks = useMemo(() => unitBlocks(plan, kept), [plan, kept]);
  const unitPaths = useMemo(
    () => new Map(plan.missingUnits.map((unit) => [unit.id, unit.path])),
    [plan.missingUnits],
  );
  const { missingUnits, missingEmployees } = plan;

  const unitColumns: TableColumnsType<MissingUnit> = [
    { title: "Подразделение", dataIndex: "path" },
    {
      title: "Решение",
      key: "decision",
      width: "30%",
      render: (_value, unit) => (
        <Radio.Group
          optionType="button"
          options={UNIT_OPTIONS}
          value={kept.has(unit.id) ? KEEP : ACT}
          onChange={(event) => onDecide([unit.id], event.target.value === KEEP)}
        />
      ),
    },
    {
      title: "Замечание",
      key: "remark",
      render: (_value, unit) => {
        const reason = blocks.get(unit.id);
        return (
          reason && (
            <Flex vertical gap="var(--ant-margin-xxs)" align="flex-start">
              <Tag color="warning">Нельзя применить</Tag>
              <span>{reason}</span>
            </Flex>
          )
        );
      },
    },
  ];

  const employeeColumns: TableColumnsType<MissingEmployee> = [
    { title: "Сотрудник", dataIndex: "fullName" },
    {
      title: "В отсутствующих подразделениях",
      key: "units",
      render: (_value, employee) => {
        const names = [...new Set(employee.orgUnitIds)]
          .map((id) => unitPaths.get(id))
          .filter((path) => path !== undefined);
        return names.length > 0 ? names.join("; ") : NO_UNITS;
      },
    },
    {
      title: "Решение",
      key: "decision",
      width: "30%",
      render: (_value, employee) => (
        <Radio.Group
          optionType="button"
          disabled={employee.isLastActiveAdministrator}
          options={EMPLOYEE_OPTIONS}
          value={isEmployeeKept(employee, kept) ? KEEP : ACT}
          onChange={(event) =>
            onDecide([employee.id], event.target.value === KEEP)
          }
        />
      ),
    },
    {
      title: "Замечание",
      key: "remark",
      render: (_value, employee) =>
        employee.isLastActiveAdministrator && (
          <Flex vertical gap="var(--ant-margin-xxs)" align="flex-start">
            <Tag color="warning">Последний администратор</Tag>
            <span>
              Уволить нельзя: это последний активный глобальный администратор.
            </span>
          </Flex>
        ),
    },
  ];

  return (
    <Flex vertical gap="middle">
      {missingUnits.length === 0 && missingEmployees.length === 0 && (
        <CompactAlert
          type="info"
          lead="Решений не требуется."
          text="Все действующие подразделения и работающие сотрудники системы есть в файле."
        />
      )}
      {missingUnits.length > 0 && (
        <Section
          title={`Подразделений нет в файле: ${missingUnits.length}`}
          actLabel="Деактивировать все"
          onAll={(isKept) =>
            onDecide(
              missingUnits.map((unit) => unit.id),
              isKept,
            )
          }
        >
          <Table<MissingUnit>
            size="small"
            className="desk-table"
            rowKey="id"
            columns={unitColumns}
            dataSource={[...missingUnits]}
            pagination={pagination}
            rowClassName={(unit) =>
              blocks.has(unit.id) ? "import-row-blocked" : ""
            }
          />
        </Section>
      )}
      {missingEmployees.length > 0 && (
        <Section
          title={`Сотрудников нет в файле: ${missingEmployees.length}`}
          actLabel="Уволить всех"
          onAll={(isKept) =>
            onDecide(
              missingEmployees.map((employee) => employee.id),
              isKept,
            )
          }
        >
          <Table<MissingEmployee>
            size="small"
            className="desk-table"
            rowKey="id"
            columns={employeeColumns}
            dataSource={[...missingEmployees]}
            pagination={pagination}
          />
        </Section>
      )}
      <ActionBar start={<Button onClick={onBack}>Назад</Button>}>
        {blocks.size > 0 && (
          <Typography.Text type="secondary">
            {`Применить нельзя: исправьте решения в отмеченных подразделениях (${blocks.size}).`}
          </Typography.Text>
        )}
        <Button type="primary" disabled={blocks.size > 0} onClick={onNext}>
          Далее
        </Button>
      </ActionBar>
    </Flex>
  );
}

interface SectionProps {
  title: string;
  /** The label of the button that applies the default decision to the whole group. */
  actLabel: string;
  onAll: (isKept: boolean) => void;
  children: ReactNode;
}

function Section({ title, actLabel, onAll, children }: SectionProps) {
  return (
    <Flex vertical gap="small">
      <Flex justify="space-between" align="center" gap="small" wrap>
        <Typography.Title level={4} style={{ margin: 0 }}>
          {title}
        </Typography.Title>
        <Flex gap="small" wrap>
          <Button onClick={() => onAll(false)}>{actLabel}</Button>
          <Button onClick={() => onAll(true)}>Оставить все</Button>
        </Flex>
      </Flex>
      {children}
    </Flex>
  );
}
