import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Empty,
  Flex,
  Space,
  Table,
  type TableColumnsType,
  Tag,
} from "antd";
import { useState } from "react";
import { Link } from "react-router";
import { ErrorAlert } from "../../app/ErrorAlert";
import { OrgUnitSelect } from "../org-structure/OrgUnitSelect";
import { useProcessRoles } from "./careerApi";
import { MatrixCreateModal } from "./MatrixCreateModal";
import { type Matrix, type MatrixListParams, matricesQuery } from "./matrixApi";
import { VERSION_STATUS_LABELS } from "./matrixLabels";

const DEFAULT_PAGE_SIZE = 20;

function versionCell(version: Matrix["inWorkVersion"]) {
  return (
    version && (
      <Space size="small">
        {`Версия ${version.number}`}
        <Tag>{VERSION_STATUS_LABELS[version.status]}</Tag>
      </Space>
    )
  );
}

const COLUMNS: TableColumnsType<Matrix> = [
  { title: "Подразделение", dataIndex: "orgUnitName" },
  { title: "Направление", dataIndex: "directionName" },
  {
    title: "Трек",
    dataIndex: "trackName",
    render: (name: string, matrix) => (
      <Link to={`/career/matrices/${matrix.id}`}>{name}</Link>
    ),
  },
  {
    title: "Опубликована",
    dataIndex: "publishedVersion",
    render: versionCell,
  },
  { title: "В работе", dataIndex: "inWorkVersion", render: versionCell },
];

/** The matrices of the units, filtered by unit; a unit head may start one. */
export function MatricesTab() {
  const roles = useProcessRoles();
  const [isCreating, setIsCreating] = useState(false);
  const [params, setParams] = useState<MatrixListParams>({
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  });
  const { data, error, isFetching, refetch } = useQuery({
    ...matricesQuery(params),
    placeholderData: (previous) => previous,
  });
  const canCreate = (roles.data?.headedUnits.length ?? 0) > 0;

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      <Flex justify="space-between" align="center" gap="middle" wrap>
        <OrgUnitSelect
          allowClear
          placeholder="Подразделение"
          onChange={(orgUnitId) =>
            setParams((previous) => ({ ...previous, orgUnitId, page: 1 }))
          }
        />
        {canCreate && (
          <Button type="primary" onClick={() => setIsCreating(true)}>
            Создать матрицу
          </Button>
        )}
      </Flex>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить матрицы"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Matrix>
        rowKey="id"
        columns={COLUMNS}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Матриц пока нет"
            />
          ),
        }}
        pagination={{
          current: params.page,
          pageSize: params.pageSize,
          total: data?.total ?? 0,
          showSizeChanger: true,
          onChange: (page, pageSize) =>
            setParams((previous) => ({ ...previous, page, pageSize })),
        }}
      />
      {isCreating && <MatrixCreateModal onClose={() => setIsCreating(false)} />}
    </Space>
  );
}
