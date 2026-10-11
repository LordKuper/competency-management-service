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
import { useProcessRoles } from "./careerApi";
import { directionsQuery, tracksQuery } from "./catalogApi";
import type { FilterOption } from "./columnFilters";
import {
  choiceFilter,
  entryOptions,
  onFilterChange,
  useUnitOptions,
} from "./columnFilters";
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

function columns(
  params: MatrixListParams,
  units: FilterOption[],
  directions: FilterOption[],
  tracks: FilterOption[],
): TableColumnsType<Matrix> {
  return [
    {
      title: "Подразделение",
      dataIndex: "orgUnitName",
      ...choiceFilter("orgUnitId", params.orgUnitId, units, true),
    },
    {
      title: "Направление",
      dataIndex: "directionName",
      ...choiceFilter("directionId", params.directionId, directions, true),
    },
    {
      title: "Трек",
      dataIndex: "trackName",
      ...choiceFilter("trackId", params.trackId, tracks, true),
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
  const units = useUnitOptions();
  const directions = entryOptions(useQuery(directionsQuery()).data?.items);
  const tracks = entryOptions(useQuery(tracksQuery()).data?.items);
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
        columns={columns(params, units, directions, tracks)}
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
