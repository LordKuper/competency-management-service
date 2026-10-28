import {
  Button,
  Col,
  Flex,
  Row,
  Select,
  Table,
  type TableColumnsType,
  Typography,
} from "antd";
import type { EmployeeOption } from "./CommissionView";
import { Panel } from "./Panel";
import type { RaterExclusions } from "./promotionApi";

type ExcludedEmployee = RaterExclusions["employees"][number];
type ExcludedPosition = RaterExclusions["positions"][number];

/** A position the search offers to forbid. */
export interface PositionOption {
  id: string;
  name: string;
}

interface ExclusionsViewProps {
  exclusions: RaterExclusions;
  /** Employees the last search found. */
  employees: readonly EmployeeOption[];
  isSearchingEmployees: boolean;
  onSearchEmployees: (text: string) => void;
  /** Positions the last search found. */
  positions: readonly PositionOption[];
  isSearchingPositions: boolean;
  onSearchPositions: (text: string) => void;
  /** Whether a change is on its way, which locks the lists until the server answers. */
  isBusy: boolean;
  onAddEmployee: (employeeId: string) => void;
  onRemoveEmployee: (employeeId: string) => void;
  onAddPosition: (positionId: string) => void;
  onRemovePosition: (positionId: string) => void;
}

interface AddSelectProps {
  label: string;
  placeholder: string;
  options: readonly { value: string; label: string; detail?: string }[];
  isSearching: boolean;
  isBusy: boolean;
  onSearch: (text: string) => void;
  onAdd: (id: string) => void;
}

function AddSelect({
  label,
  placeholder,
  options,
  isSearching,
  isBusy,
  onSearch,
  onAdd,
}: AddSelectProps) {
  return (
    <Select<string | null>
      showSearch={{ filterOption: false, onSearch }}
      aria-label={label}
      placeholder={placeholder}
      value={null}
      options={[...options]}
      loading={isSearching}
      disabled={isBusy}
      optionRender={(option) => (
        <Flex vertical>
          <span>{option.label}</span>
          <Typography.Text type="secondary">
            {options.find((item) => item.value === option.value)?.detail}
          </Typography.Text>
        </Flex>
      )}
      notFoundContent="Ничего не найдено. Измените запрос."
      onSelect={(id) => id !== null && onAdd(id)}
    />
  );
}

/** The employees and the positions the administrator forbids to name as raters: each list has a search to add to it and a table to take from it. */
export function ExclusionsView({
  exclusions,
  employees,
  isSearchingEmployees,
  onSearchEmployees,
  positions,
  isSearchingPositions,
  onSearchPositions,
  isBusy,
  onAddEmployee,
  onRemoveEmployee,
  onAddPosition,
  onRemovePosition,
}: ExclusionsViewProps) {
  const forbiddenEmployees = new Set(exclusions.employees.map(({ id }) => id));
  const forbiddenPositions = new Set(exclusions.positions.map(({ id }) => id));
  const employeeColumns: TableColumnsType<ExcludedEmployee> = [
    { title: "Сотрудник", dataIndex: "fullName" },
    { title: "Подразделение", dataIndex: "unitName" },
    {
      title: "Действия",
      key: "actions",
      render: (_, employee) => (
        <Button
          type="link"
          disabled={isBusy}
          aria-label={`Убрать из запрета: ${employee.fullName}`}
          onClick={() => onRemoveEmployee(employee.id)}
        >
          Убрать
        </Button>
      ),
    },
  ];
  const positionColumns: TableColumnsType<ExcludedPosition> = [
    { title: "Должность", dataIndex: "name" },
    { title: "Сотрудников", dataIndex: "employeeCount", align: "right" },
    {
      title: "Действия",
      key: "actions",
      render: (_, position) => (
        <Button
          type="link"
          disabled={isBusy}
          aria-label={`Убрать из запрета: ${position.name}`}
          onClick={() => onRemovePosition(position.id)}
        >
          Убрать
        </Button>
      ),
    },
  ];
  return (
    <Panel title="Кого нельзя указывать оценивающим">
      <Flex vertical gap="middle">
        <Typography.Text type="secondary">
          Этих сотрудников и всех на этих должностях нельзя включить в список
          оценивающих и пригласить в систему. Кандидат, руководитель и HR увидят
          в поиске, что указывать их запрещено.
        </Typography.Text>
        <Row gutter={[24, 24]}>
          <Col xs={24} lg={12}>
            <Flex vertical gap="small">
              <AddSelect
                label="Добавить сотрудника"
                placeholder="Добавить сотрудника: начните вводить ФИО"
                options={employees
                  .filter(({ id }) => !forbiddenEmployees.has(id))
                  .map((employee) => ({
                    value: employee.id,
                    label: employee.name,
                    detail: employee.detail,
                  }))}
                isSearching={isSearchingEmployees}
                isBusy={isBusy}
                onSearch={onSearchEmployees}
                onAdd={onAddEmployee}
              />
              <Table<ExcludedEmployee>
                rowKey="id"
                columns={employeeColumns}
                dataSource={[...exclusions.employees]}
                pagination={false}
                scroll={{ x: "max-content" }}
                locale={{ emptyText: "Сотрудников в запрете нет." }}
              />
            </Flex>
          </Col>
          <Col xs={24} lg={12}>
            <Flex vertical gap="small">
              <AddSelect
                label="Добавить должность"
                placeholder="Добавить должность: начните вводить название"
                options={positions
                  .filter(({ id }) => !forbiddenPositions.has(id))
                  .map((position) => ({
                    value: position.id,
                    label: position.name,
                  }))}
                isSearching={isSearchingPositions}
                isBusy={isBusy}
                onSearch={onSearchPositions}
                onAdd={onAddPosition}
              />
              <Table<ExcludedPosition>
                rowKey="id"
                columns={positionColumns}
                dataSource={[...exclusions.positions]}
                pagination={false}
                scroll={{ x: "max-content" }}
                locale={{ emptyText: "Должностей в запрете нет." }}
              />
            </Flex>
          </Col>
        </Row>
      </Flex>
    </Panel>
  );
}
