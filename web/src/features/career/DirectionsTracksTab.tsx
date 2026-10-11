import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Flex, Space, Table, type TableColumnsType } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import {
  ALL_ENTRIES,
  type CareerTrack,
  type CatalogEntry,
  type CatalogListParams,
  directionsQuery,
  type TrackFilters,
  tracksQuery,
} from "./catalogApi";
import { onFilterChange } from "./columnFilters";
import { NameModal } from "./NameModal";
import { LevelsTable } from "./TrackLevels";
import { TrackModal } from "./TrackModal";
import { unitFilter } from "./UnitTreeFilter";
import { useCatalogRights } from "./useCatalogRights";

/** Dialog of a table: creating (`"create"`) or editing the row with that id; null when closed. */
type Dialog = "create" | string | null;

const NAME_COLUMN = { title: "Название", dataIndex: "name" };

/** Directions with their tracks and the tracks' levels, nested as expandable rows. */
export function DirectionsTracksTab() {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [params, setParams] = useState<CatalogListParams>(ALL_ENTRIES);
  const { data, error, isFetching, refetch } = useQuery(
    directionsQuery(params),
  );
  const edited = data?.items.find((direction) => direction.id === dialog);
  const columns: TableColumnsType<CatalogEntry> = [
    NAME_COLUMN,
    ...entryColumns<CatalogEntry>(params.status),
    {
      title: "Действия",
      key: "actions",
      render: (_, direction) => (
        <EntryActions
          segment="directions"
          entry={direction}
          onEdit={() => setDialog(direction.id)}
        />
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="large" style={{ display: "flex" }}>
      {rights.canCreate && (
        <Flex>
          <Button type="primary" onClick={() => setDialog("create")}>
            Добавить направление
          </Button>
        </Flex>
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить направления"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CatalogEntry>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setParams)}
        expandable={{
          expandedRowRender: (direction) => (
            <TracksTable direction={direction} />
          ),
        }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="Направлений пока нет"
            />
          ),
        }}
      />
      {dialog === "create" && (
        <NameModal
          segment="directions"
          title="Новое направление"
          onClose={() => setDialog(null)}
        />
      )}
      {edited && (
        <NameModal
          segment="directions"
          title="Изменение направления"
          entry={edited}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}

function TracksTable({ direction }: { direction: CatalogEntry }) {
  const rights = useCatalogRights();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [filters, setFilters] = useState<TrackFilters>({});
  const { data, error, isFetching, refetch } = useQuery(
    tracksQuery(direction.id, filters),
  );
  const edited = data?.items.find((track) => track.id === dialog);
  const columns: TableColumnsType<CareerTrack> = [
    NAME_COLUMN,
    {
      title: "Владелец",
      dataIndex: "ownerOrgUnitName",
      ...unitFilter("ownerOrgUnitId", filters.ownerOrgUnitId),
      render: (name: string) => `Подразделение: ${name}`,
    },
    ...entryColumns<CareerTrack>(filters.status),
    {
      title: "Действия",
      key: "actions",
      render: (_, track) => (
        <EntryActions
          segment="tracks"
          entry={track}
          unitId={track.ownerOrgUnitId}
          onEdit={() => setDialog(track.id)}
        />
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="middle" style={{ display: "flex" }}>
      {rights.canCreateTrack && (
        <Flex>
          <Button onClick={() => setDialog("create")}>Добавить трек</Button>
        </Flex>
      )}
      {error && (
        <ErrorAlert
          title="Не удалось загрузить треки"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CareerTrack>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        onChange={onFilterChange(setFilters)}
        expandable={{
          expandedRowRender: (track) => (
            <LevelsTable track={track} editable={false} />
          ),
        }}
      />
      {dialog === "create" && (
        <TrackModal
          directionId={direction.id}
          onClose={() => setDialog(null)}
        />
      )}
      {edited && (
        <TrackModal
          directionId={direction.id}
          track={edited}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
