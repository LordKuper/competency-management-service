import { useQuery } from "@tanstack/react-query";
import {
  theme as antdTheme,
  Checkbox,
  Col,
  Empty,
  Input,
  Row,
  Select,
  Space,
  Table,
  type TableColumnsType,
} from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useIsAdmin } from "../auth/useCurrentUser";
import { OrgUnitSelect } from "./OrgUnitSelect";
import {
  type Employee,
  type EmployeeListParams,
  employeeListQuery,
} from "./orgStructureApi";
import { paths } from "./paths";
import { EmployeeStatusTag } from "./StatusTags";

const DEFAULT_PAGE_SIZE = 20;

const STATUS_OPTIONS = [
  { value: true, label: "Работают" },
  { value: false, label: "Не работают" },
];

interface EmployeeListProps {
  /** Unit whose employees are listed; omitted to list everyone, with a unit filter offered instead. */
  unitId?: string;
}

/**
 * Employees with search, paging and, for administrators, the status filter and the hidden columns.
 * Ordinary users are given only the restricted projection, so the controls for what it lacks are not shown.
 * Mount it with a `key` per unit, so the filters start over for another unit.
 */
export function EmployeeList({ unitId }: EmployeeListProps) {
  const { token } = antdTheme.useToken();
  const isAdmin = useIsAdmin();
  const [filters, setFilters] = useState<EmployeeListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const params = { ...filters, orgUnitId: unitId ?? filters.orgUnitId };
  const { data, error, isFetching, refetch } = useQuery(
    employeeListQuery(params),
  );

  const columns: TableColumnsType<Employee> = [
    {
      title: "ФИО",
      dataIndex: "fullName",
      render: (fullName: string, employee) => (
        <Link to={paths.employee(employee.id)}>{fullName}</Link>
      ),
    },
    { title: "E-mail", dataIndex: "email" },
    { title: "Подразделение", dataIndex: "orgUnitName" },
    { title: "Должность", dataIndex: "position" },
    ...(isAdmin
      ? [
          { title: "Табельный номер", dataIndex: "personnelNumber" },
          {
            title: "Статус",
            dataIndex: "isActive",
            render: (isActive: boolean | null | undefined) =>
              isActive != null && <EmployeeStatusTag isActive={isActive} />,
          },
        ]
      : []),
  ];

  return (
    <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
      <Row gutter={[token.margin, token.margin]} align="middle">
        <Col xs={24} md={12} lg={8}>
          <Input.Search
            allowClear
            aria-label="Поиск сотрудников"
            placeholder={
              isAdmin ? "ФИО, e-mail или табельный номер" : "ФИО или e-mail"
            }
            onSearch={(text) =>
              setFilters((previous) => ({
                ...previous,
                q: text.trim() || undefined,
                page: 1,
              }))
            }
          />
        </Col>
        {isAdmin && (
          <Col xs={12} md={6} lg={4}>
            <Select
              allowClear
              aria-label="Фильтр по статусу"
              placeholder="Статус"
              options={STATUS_OPTIONS}
              style={{ width: "100%" }}
              onChange={(isActive?: boolean) =>
                setFilters((previous) => ({ ...previous, isActive, page: 1 }))
              }
            />
          </Col>
        )}
        {unitId === undefined && (
          <Col xs={12} md={12} lg={8}>
            <OrgUnitSelect
              allowClear
              aria-label="Фильтр по подразделению"
              placeholder="Подразделение"
              onChange={(orgUnitId) =>
                setFilters((previous) => ({ ...previous, orgUnitId, page: 1 }))
              }
            />
          </Col>
        )}
        {params.orgUnitId && (
          <Col xs={24} lg={8}>
            <Checkbox
              checked={filters.includeDescendants ?? false}
              onChange={(event) =>
                setFilters((previous) => ({
                  ...previous,
                  includeDescendants: event.target.checked,
                  page: 1,
                }))
              }
            >
              Включая вложенные подразделения
            </Checkbox>
          </Col>
        )}
      </Row>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить сотрудников"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Employee>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Сотрудники не найдены. Измените условия поиска."
            />
          ),
        }}
        pagination={{
          current: params.page,
          pageSize: params.pageSize,
          total: data?.total ?? 0,
          showSizeChanger: true,
          onChange: (page, pageSize) =>
            setFilters((previous) => ({ ...previous, page, pageSize })),
        }}
      />
    </Space>
  );
}
