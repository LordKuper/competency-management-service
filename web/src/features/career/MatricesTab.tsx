import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Empty,
  Flex,
  Select,
  Space,
  Table,
  type TableColumnsType,
  Tag,
  theme,
} from "antd";
import type { FilterDropdownProps } from "antd/es/table/interface";
import { type Dispatch, type SetStateAction, useState } from "react";
import { Link } from "react-router";
import { api } from "../../api/client";
import { unwrap } from "../../api/unwrap";
import { ErrorAlert } from "../../app/ErrorAlert";
import { useDebouncedValue } from "../../app/useDebouncedValue";
import {
  CatalogSelect,
  catalogFilter,
  FILTER_WIDTH_IN_CONTROLS,
} from "./CatalogSelect";
import { useProcessRoles } from "./careerApi";
import { onFilterChange, pageProps } from "./columnFilters";
import { MatrixCreateModal } from "./MatrixCreateModal";
import { MatrixDeleteButton } from "./MatrixDeleteButton";
import { type Matrix, type MatrixListParams, matricesQuery } from "./matrixApi";
import { VERSION_STATUS_LABELS } from "./matrixLabels";
import { unitFilter } from "./UnitTreeFilter";

const POSITION_SEARCH_PAGE = 20;

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

interface TrackOrPositionFilterProps extends FilterDropdownProps {
  params: MatrixListParams;
  setParams: Dispatch<SetStateAction<MatrixListParams>>;
}

/** The filter of the «Трек или должность» column: a track like any column filter, or a position searched on the server. */
function TrackOrPositionFilter({
  params,
  setParams,
  setSelectedKeys,
  confirm,
}: TrackOrPositionFilterProps) {
  const { token } = theme.useToken();
  const [search, setSearch] = useState("");
  const q = useDebouncedValue(search.trim());
  const positions = useQuery({
    queryKey: ["career", "matrices", "position-filter", q],
    queryFn: async () =>
      unwrap(
        await api.GET("/api/v1/positions", {
          params: {
            query: {
              q: q || undefined,
              isActive: true,
              page: 1,
              pageSize: POSITION_SEARCH_PAGE,
            },
          },
        }),
      ),
  });
  const width = token.controlHeight * FILTER_WIDTH_IN_CONTROLS;
  const container = (node: HTMLElement) => node.parentElement ?? document.body;
  return (
    <Flex vertical gap="small" style={{ padding: token.paddingXS }}>
      <CatalogSelect
        segment="tracks"
        aria-label="Трек"
        placeholder="Трек"
        allowClear
        value={params.trackId}
        style={{ width }}
        getPopupContainer={container}
        onChange={(value) => {
          setSelectedKeys(value ? [value] : []);
          setParams((previous) => ({ ...previous, positionId: undefined }));
          confirm();
        }}
      />
      <Select
        showSearch={{ filterOption: false, onSearch: setSearch }}
        allowClear
        aria-label="Должность"
        placeholder="Должность"
        value={params.positionId}
        loading={positions.isFetching}
        style={{ width }}
        getPopupContainer={container}
        options={(positions.data?.items ?? []).map((position) => ({
          value: position.id,
          label: position.name,
        }))}
        onChange={(value: string | undefined) => {
          setParams((previous) => ({
            ...previous,
            positionId: value,
            trackId: undefined,
            page: 1,
          }));
          confirm();
        }}
      />
    </Flex>
  );
}

function columns(
  params: MatrixListParams,
  setParams: Dispatch<SetStateAction<MatrixListParams>>,
): TableColumnsType<Matrix> {
  return [
    {
      title: "Подразделение",
      dataIndex: "orgUnitName",
      ...unitFilter("orgUnitId", params.orgUnitId),
    },
    {
      title: "Вид",
      key: "kind",
      render: (_, matrix) =>
        matrix.positionId ? (
          <Tag color="processing">Должность</Tag>
        ) : (
          <Tag>Трек</Tag>
        ),
    },
    {
      title: "Область",
      dataIndex: "directionName",
      ...catalogFilter(
        "directionId",
        "directions",
        params.directionId,
        "Область",
      ),
      render: (name: string | null) => name ?? "—",
    },
    {
      title: "Трек или должность",
      key: "trackId",
      filtered: params.trackId !== undefined || params.positionId !== undefined,
      filteredValue: params.trackId === undefined ? null : [params.trackId],
      filterMultiple: false,
      filterDropdown: (props) => (
        <TrackOrPositionFilter
          {...props}
          params={params}
          setParams={setParams}
        />
      ),
      render: (_, matrix) => (
        <Link to={`/career/matrices/${matrix.id}`}>
          {matrix.trackName ?? matrix.positionName}
        </Link>
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

/** The matrices of the units, filtered by unit, direction, track and position in the column headers; a unit head may start one. */
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
  const canCreate = (roles.data?.matrixUnits.length ?? 0) > 0;

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
        columns={columns(params, setParams)}
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
