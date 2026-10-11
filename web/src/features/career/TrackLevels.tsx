import { useQuery } from "@tanstack/react-query";
import { Button, Empty, Flex, Space, Table, type TableColumnsType } from "antd";
import { useState } from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { EntryActions, entryColumns } from "./CatalogParts";
import { type CareerLevel, type CareerTrack, levelsQuery } from "./catalogApi";
import { LevelModal, type LevelValues } from "./LevelModal";

const BASE_COLUMNS: TableColumnsType<CareerLevel> = [
  { title: "Название", dataIndex: "name" },
  { title: "Код уровня", dataIndex: "code" },
  ...entryColumns<CareerLevel>(),
];

/** The saved levels of a track. With `editable` it offers the level actions; without, it only shows them. */
export function LevelsTable({
  track,
  editable,
}: {
  track: CareerTrack;
  editable: boolean;
}) {
  const [editedId, setEditedId] = useState<string | null>(null);
  const { data, error, isFetching, refetch } = useQuery(levelsQuery(track.id));
  const edited = data?.items.find((level) => level.id === editedId);
  const columns: TableColumnsType<CareerLevel> = editable
    ? [
        ...BASE_COLUMNS,
        {
          title: "Действия",
          key: "actions",
          render: (_, level) => (
            <EntryActions
              segment="levels"
              entry={level}
              unitId={track.ownerOrgUnitId}
              onEdit={() => setEditedId(level.id)}
            />
          ),
        },
      ]
    : BASE_COLUMNS;

  return (
    <>
      {error && (
        <ErrorAlert
          title="Не удалось загрузить уровни"
          error={error}
          onRetry={() => void refetch()}
        />
      )}
      <Table<CareerLevel>
        rowKey="id"
        columns={columns}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        locale={{
          emptyText: (
            <Empty
              image={Empty.PRESENTED_IMAGE_SIMPLE}
              description="В треке пока нет уровней"
            />
          ),
        }}
      />
      {edited && (
        <LevelModal
          trackId={track.id}
          level={edited}
          onClose={() => setEditedId(null)}
        />
      )}
    </>
  );
}

interface PendingLevelsProps {
  levels: readonly LevelValues[];
  onChange: (levels: LevelValues[]) => void;
}

/** Levels added in the track dialog that are saved with the track: add, edit and remove before «Сохранить». */
export function PendingLevels({ levels, onChange }: PendingLevelsProps) {
  /** The level being edited by its place in the list, `"new"` for a level being added, null when no dialog is open. */
  const [dialog, setDialog] = useState<number | "new" | null>(null);
  const columns: TableColumnsType<LevelValues> = [
    { title: "Название", dataIndex: "name" },
    { title: "Код уровня", dataIndex: "code" },
    {
      title: "Действия",
      key: "actions",
      render: (_, __, index) => (
        <Space size={0}>
          <Button type="link" size="small" onClick={() => setDialog(index)}>
            Изменить
          </Button>
          <Button
            type="link"
            size="small"
            danger
            onClick={() => onChange(levels.filter((_, i) => i !== index))}
          >
            Удалить
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <Space orientation="vertical" size="small" style={{ display: "flex" }}>
      {levels.length > 0 && (
        <Table<LevelValues>
          rowKey={(_, index) => String(index)}
          columns={columns}
          dataSource={[...levels]}
          pagination={false}
          size="small"
          scroll={{ x: "max-content" }}
        />
      )}
      <Flex>
        <Button onClick={() => setDialog("new")}>Добавить уровень</Button>
      </Flex>
      {dialog !== null && (
        <LevelModal
          draft={dialog === "new" ? undefined : levels[dialog]}
          onCollect={(values) =>
            onChange(
              dialog === "new"
                ? [...levels, values]
                : levels.map((level, i) => (i === dialog ? values : level)),
            )
          }
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
