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
import { catalogFilter } from "./CatalogSelect";
import { useProcessRoles } from "./careerApi";
import { onFilterChange, pageProps } from "./columnFilters";
import { MatrixCreateModal } from "./MatrixCreateModal";
import { MatrixDeleteButton } from "./MatrixDeleteButton";
import { type Matrix, type MatrixListParams, matricesQuery } from "./matrixApi";
import { VERSION_STATUS_LABELS } from "./matrixLabels";
import { unitFilter } from "./UnitTreeFilter";

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

function columns(params: MatrixListParams): TableColumnsType<Matrix> {
  return [
    {
      title: "Подразделение",
      dataIndex: "orgUnitName",
      ...unitFilter("orgUnitId", params.orgUnitId),
    },
    {
      title: "Направление",
      dataIndex: "directionName",
      ...catalogFilter(
        "directionId",
        "directions",
        params.directionId,
        "Направление",
      ),
    },
    {
      title: "Трек",
      dataIndex: "trackName",
      ...catalogFilter("trackId", "tracks", params.trackId, "Трек"),
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
    {
      title: "Действия",
      key: "actions",
      render: (_, matrix) => <MatrixDeleteButton matrix={matrix} />,
    },
  ];
}

/** The matrices of the units, filtered by unit, direction and track in the column headers; a unit head may start one. */
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
      {canCreate && (
        <Flex>
          <Button type="primary" onClick={() => setIsCreating(true)}>
            Создать матрицу
          </Button>
        </Flex>
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить матрицы"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<Matrix>
        rowKey="id"
        columns={columns(params)}
        dataSource={data?.items}
        loading={isFetching}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Матриц пока нет"
            />
          ),
        }}
        pagination={pageProps(params, data?.total, setParams)}
      />
      {isCreating && <MatrixCreateModal onClose={() => setIsCreating(false)} />}
    </Space>
  );
}
