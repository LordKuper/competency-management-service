import { HolderOutlined } from "@ant-design/icons";
import {
  type Announcements,
  closestCenter,
  DndContext,
  type DragEndEvent,
  KeyboardSensor,
  type Modifier,
  PointerSensor,
  type ScreenReaderInstructions,
  type UniqueIdentifier,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { useQuery } from "@tanstack/react-query";
import {
  Button,
  Empty,
  Flex,
  Space,
  Table,
  type TableColumnsType,
  Typography,
  theme,
} from "antd";
import {
  type CSSProperties,
  createContext,
  type HTMLAttributes,
  useContext,
  useMemo,
  useState,
} from "react";
import { ErrorAlert } from "../../app/ErrorAlert";
import { CatalogStatusTag, EntryActions, entryColumns } from "./CatalogParts";
import { type CareerLevel, type CareerTrack, levelsQuery } from "./catalogApi";
import { LevelModal, type LevelValues } from "./LevelModal";

const EMPTY_TEXT = "В треке пока нет уровней";

/** Counts the levels added in this session, so each unsaved level has a key of its own. */
let pendingCount = 0;

function EmptyLevels() {
  return (
    <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={EMPTY_TEXT} />
  );
}

const READ_ONLY_COLUMNS: TableColumnsType<CareerLevel> = [
  { title: "Название", dataIndex: "name" },
  ...entryColumns<CareerLevel>(),
];

/** The saved levels of a track in track order, for reading. */
export function LevelsTable({ track }: { track: CareerTrack }) {
  const { data, error, isFetching, refetch } = useQuery(levelsQuery(track.id));

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
        columns={READ_ONLY_COLUMNS}
        dataSource={data?.items}
        loading={isFetching}
        pagination={false}
        scroll={{ x: "max-content" }}
        locale={{ emptyText: <EmptyLevels /> }}
      />
    </>
  );
}

/** A level added in the track dialog and saved with the track; the key stands for it in the order until it is saved. */
export interface PendingLevel {
  key: string;
  values: LevelValues;
}

/** A row of the track dialog's level list: a saved level, keyed by its id, or a pending one. */
export interface LevelRow {
  key: string;
  name: string;
  level?: CareerLevel;
  pending?: PendingLevel;
}

/**
 * The saved levels in track order followed by the pending ones, rearranged by `order`, the row keys as the user last
 * arranged them; rows the order does not name yet keep their place after the named ones.
 */
export function arrangeLevels(
  saved: readonly CareerLevel[],
  pending: readonly PendingLevel[],
  order: readonly string[] | undefined,
): LevelRow[] {
  const rows: LevelRow[] = [
    ...saved.map((level) => ({ key: level.id, name: level.name, level })),
    ...pending.map((item) => ({
      key: item.key,
      name: item.values.name,
      pending: item,
    })),
  ];
  if (!order) return rows;
  const rank = (row: LevelRow) => {
    const index = order.indexOf(row.key);
    return index === -1 ? order.length : index;
  };
  return rows.sort((left, right) => rank(left) - rank(right));
}

const restrictToVerticalAxis: Modifier = ({ transform }) => ({
  ...transform,
  x: 0,
});

const screenReaderInstructions: ScreenReaderInstructions = {
  draggable:
    "Чтобы взять уровень, нажмите пробел или Enter. Перемещайте его стрелками вверх и вниз, отпустите пробелом или Enter, Escape — отмена",
};

/** What the screen reader hears while a level is moved, by name and by place from 1, in the order before the move. */
export function levelAnnouncements(rows: readonly LevelRow[]): Announcements {
  const position = (id: UniqueIdentifier) =>
    rows.findIndex((row) => row.key === id) + 1;
  const name = (id: UniqueIdentifier) =>
    rows.find((row) => row.key === id)?.name ?? "";
  const total = rows.length;
  return {
    onDragStart: ({ active }) =>
      `Взят уровень «${name(active.id)}», позиция ${position(active.id)} из ${total}`,
    onDragOver: ({ active, over }) =>
      over
        ? `Уровень «${name(active.id)}» на позиции ${position(over.id)} из ${total}`
        : undefined,
    onDragEnd: ({ active, over }) =>
      over
        ? `Уровень «${name(active.id)}» перемещён на позицию ${position(over.id)} из ${total}`
        : undefined,
    onDragCancel: ({ active }) =>
      `Перемещение отменено. Уровень «${name(active.id)}» остался на позиции ${position(active.id)}`,
  };
}

/** The row keys after a level was dropped onto another's place; null when it was dropped where it was or nowhere. */
export function movedOrder(
  rows: readonly LevelRow[],
  { active, over }: Pick<DragEndEvent, "active" | "over">,
): string[] | null {
  const keys = rows.map((row) => row.key);
  if (!over || active.id === over.id) return null;
  return arrayMove(
    keys,
    keys.indexOf(String(active.id)),
    keys.indexOf(String(over.id)),
  );
}

type Handle = Pick<
  ReturnType<typeof useSortable>,
  "attributes" | "listeners" | "setActivatorNodeRef"
>;

const HandleContext = createContext<Handle | null>(null);

type RowProps = HTMLAttributes<HTMLTableRowElement> & {
  "data-row-key"?: string;
};

/** A table row that can be moved by its handle; the placeholder row of an empty table has no key and stays a plain row. */
function SortableRow(props: RowProps) {
  const id = props["data-row-key"];
  if (id === undefined) return <tr {...props} />;
  return <SortableRowWithId id={id} {...props} />;
}

function SortableRowWithId({ id, ...props }: RowProps & { id: string }) {
  const { token } = theme.useToken();
  const {
    attributes,
    listeners,
    setNodeRef,
    setActivatorNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id,
    attributes: { roleDescription: "перемещаемый элемент" },
    transition: token.motion ? undefined : null,
  });
  const handle = useMemo(
    () => ({ attributes, listeners, setActivatorNodeRef }),
    [attributes, listeners, setActivatorNodeRef],
  );
  const style: CSSProperties = {
    ...props.style,
    transform: CSS.Translate.toString(transform),
    transition,
    ...(isDragging ? { position: "relative", zIndex: 1 } : {}),
  };

  return (
    <HandleContext.Provider value={handle}>
      <tr {...props} ref={setNodeRef} style={style} />
    </HandleContext.Provider>
  );
}

function DragHandle({ name }: { name: string }) {
  const handle = useContext(HandleContext);
  return (
    <Button
      type="text"
      size="small"
      icon={<HolderOutlined />}
      aria-label={`Перетащить уровень «${name}»`}
      ref={handle?.setActivatorNodeRef}
      {...handle?.attributes}
      {...handle?.listeners}
    />
  );
}

interface TrackLevelsProps {
  /** The track as saved; omitted while it is not saved yet. */
  track?: CareerTrack;
  rows: readonly LevelRow[];
  isLoading: boolean;
  error: unknown;
  onRetry: () => void;
  pending: readonly PendingLevel[];
  onPendingChange: (pending: PendingLevel[]) => void;
  onOrderChange: (order: string[]) => void;
}

/**
 * The levels of the track dialog, saved and pending, from the most junior: pending ones are added, renamed and removed
 * before «Сохранить», saved ones take their actions at once, and every row is moved by its handle, also from the keyboard.
 */
export function TrackLevels({
  track,
  rows,
  isLoading,
  error,
  onRetry,
  pending,
  onPendingChange,
  onOrderChange,
}: TrackLevelsProps) {
  const { token } = theme.useToken();
  const [dialog, setDialog] = useState<string | null>(null);
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );
  const edited = rows.find((row) => row.key === dialog);
  const columns: TableColumnsType<LevelRow> = [
    {
      key: "handle",
      width: token.controlHeight,
      render: (_, row) => <DragHandle name={row.name} />,
    },
    { title: "Название", dataIndex: "name" },
    {
      title: "Статус",
      key: "status",
      render: (_, row) =>
        row.level && <CatalogStatusTag status={row.level.status} />,
    },
    {
      title: "Действия",
      key: "actions",
      render: (_, row) =>
        row.level ? (
          <EntryActions
            segment="levels"
            entry={row.level}
            unitId={track?.ownerOrgUnitId}
            onEdit={() => setDialog(row.key)}
          />
        ) : (
          <Space size={0}>
            <Button type="link" size="small" onClick={() => setDialog(row.key)}>
              Изменить
            </Button>
            <Button
              type="link"
              size="small"
              danger
              onClick={() =>
                onPendingChange(pending.filter((item) => item.key !== row.key))
              }
            >
              Удалить
            </Button>
          </Space>
        ),
    },
  ];

  function collect(values: LevelValues) {
    onPendingChange(
      dialog === "new"
        ? [...pending, { key: `pending-${++pendingCount}`, values }]
        : pending.map((item) =>
            item.key === dialog ? { ...item, values } : item,
          ),
    );
  }

  return (
    <Space orientation="vertical" size="small" style={{ display: "flex" }}>
      <Typography.Text type="secondary">
        Уровни идут от младшего к старшему. Перетащите строку, чтобы изменить
        порядок
      </Typography.Text>
      {error != null && (
        <ErrorAlert
          title="Не удалось загрузить уровни"
          error={error}
          onRetry={onRetry}
        />
      )}
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        modifiers={[restrictToVerticalAxis]}
        accessibility={{
          announcements: levelAnnouncements(rows),
          screenReaderInstructions,
        }}
        onDragEnd={(event) => {
          const order = movedOrder(rows, event);
          if (order) onOrderChange(order);
        }}
      >
        <SortableContext
          items={rows.map((row) => row.key)}
          strategy={verticalListSortingStrategy}
        >
          <Table<LevelRow>
            rowKey="key"
            components={{ body: { row: SortableRow } }}
            columns={columns}
            dataSource={[...rows]}
            loading={isLoading}
            pagination={false}
            size="small"
            scroll={{ x: "max-content" }}
            locale={{ emptyText: <EmptyLevels /> }}
          />
        </SortableContext>
      </DndContext>
      <Flex>
        <Button onClick={() => setDialog("new")}>Добавить уровень</Button>
      </Flex>
      {edited?.level && (
        <LevelModal level={edited.level} onClose={() => setDialog(null)} />
      )}
      {(dialog === "new" || edited?.pending) && (
        <LevelModal
          draft={edited?.pending?.values}
          onCollect={collect}
          onClose={() => setDialog(null)}
        />
      )}
    </Space>
  );
}
